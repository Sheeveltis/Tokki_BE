using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.GoogleLogin
{
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IEmailService _emailService;
        private readonly GoogleAuthSettings _googleSettings;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IEmailService emailService,
            IOptions<GoogleAuthSettings> googleOptions,
            ILogger<GoogleLoginCommandHandler> logger)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _emailService = emailService;
            _googleSettings = googleOptions.Value;
            _logger = logger;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            GoogleLoginCommand request,
            CancellationToken cancellationToken)
        {
            var nowLocal = DateTime.UtcNow.AddHours(7);

            // 1) Validate token + get payload
            GoogleJsonWebSignature.Payload? payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = _googleSettings.ClientIds,
                        IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                        ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                    });

                if (payload == null)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.InvalidGoogleToken },
                        401,
                        "Google token không hợp lệ");
                }

                if (string.IsNullOrWhiteSpace(payload.Subject))
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.InvalidGoogleToken },
                        401,
                        "Google User ID không hợp lệ");
                }

                if (string.IsNullOrWhiteSpace(payload.Email))
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.GoogleEmailRequired },
                        400,
                        "Email là bắt buộc. Vui lòng cấp quyền email cho ứng dụng");
                }

                // Hardening: yêu cầu email đã verify
                if (!payload.EmailVerified)
                {
                    return OperationResult<LoginResponse>.Failure(
                        "Email Google chưa được xác thực. Vui lòng xác thực email trên Google trước khi đăng nhập.",
                        400);
                }

            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google JWT token");
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidGoogleToken },
                    401,
                    "Google token không hợp lệ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google authentication failed.");
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidGoogleToken },
                    401,
                    "Google authentication failed");
            }

            // 2) Check SocialLogin by GoogleId first
            var socialLogin = await _socialLoginRepo.GetByProviderAsync("google", payload.Subject);

            Account user;
            bool isNewAccount = false;

            // For email notification after SaveChanges
            string? defaultPasswordToEmail = null;
            string? emailToNotify = null;
            string? nameToNotify = null;

            if (socialLogin != null)
            {
                // Đã có SocialLogin -> login bình thường
                user = await _accountRepo.GetByIdAsync(socialLogin.UserId);

                if (user == null)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.UserNotFoundById },
                        404,
                        "Tài khoản không tồn tại.");
                }
                

                // Check status
                var statusCheck = CheckAccountStatus(user, nowLocal);
                if (statusCheck != null) return statusCheck;
            }
            else
            {
                // 3) No SocialLogin yet -> check by email
                user = await _accountRepo.GetByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Email chưa tồn tại -> tạo account mới
                    string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER");
                    if (string.IsNullOrWhiteSpace(defaultPassword))
                    {
                        _logger.LogError("Cấu hình DEFAULT_PASSWORD_FOR_USER không được tìm thấy hoặc rỗng.");
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.ServerError },
                            500,
                            "Cấu hình mật khẩu mặc định cho User chưa được thiết lập.");
                    }

                    try
                    {
                        string passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                        user = new Account
                        {
                            UserId = _idGenerator.Generate(15),
                            Email = payload.Email,
                            FullName = payload.Name ?? payload.Email,
                            PasswordHash = passwordHash,
                            Role = AccountRole.User,
                            AvatarUrl = payload.Picture,
                            Status = AccountStatus.Active,
                            CreatedAt = nowLocal,
                            UpdatedAt = nowLocal,
                            LastLoginAt = nowLocal
                        };

                        await _accountRepo.AddAsync(user);

                        await _socialLoginRepo.AddAsync(new SocialLogin
                        {
                            Id = _idGenerator.Generate(15),
                            UserId = user.UserId,
                            Provider = "google",
                            ProviderUserId = payload.Subject,
                            EmailFromProvider = payload.Email,
                            NameFromProvider = payload.Name
                        });

                        isNewAccount = true;

                        defaultPasswordToEmail = defaultPassword;
                        emailToNotify = payload.Email;
                        nameToNotify = payload.Name ?? payload.Email;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo tài khoản Google.");
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.ServerError },
                            500,
                            ex.Message);
                    }
                }
                else
                {
                    // Email đã tồn tại -> merge/link Google
                    var statusCheck = CheckAccountStatus(user, nowLocal);
                    if (statusCheck != null) return statusCheck;

                    if (!request.IsComfirmToMergeAcc)
                    {
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.MergeAccountRequered },
                            409,
                            "Account merge confirmation required");
                    }

                    // Link social login
                    try
                    {
                        await _socialLoginRepo.AddAsync(new SocialLogin
                        {
                            Id = _idGenerator.Generate(15),
                            UserId = user.UserId,
                            Provider = "google",
                            ProviderUserId = payload.Subject,
                            EmailFromProvider = payload.Email,
                            NameFromProvider = payload.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra khi liên kết tài khoản Google.");
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.ServerError },
                            500,
                            "Không thể liên kết tài khoản Google.");
                    }
                }
            }

            // 4) Update last login (tránh state issue: new user không gọi UpdateUserAsync)
            user.LastLoginAt = nowLocal;
            user.UpdatedAt = nowLocal;

            try
            {
                if (!isNewAccount)
                {
                    await _accountRepo.UpdateUserAsync(user);
                }

                await _accountRepo.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông tin đăng nhập Google.");
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    "Không thể lưu thông tin đăng nhập.");
            }

            // 5) Send email after commit (new account) + message rõ nếu fail
            bool emailSent = true;
            if (isNewAccount &&
                !string.IsNullOrWhiteSpace(defaultPasswordToEmail) &&
                !string.IsNullOrWhiteSpace(emailToNotify))
            {
                try
                {
                    await _emailService.SendGoogleAccountInfoAsync(
                        emailToNotify,
                        nameToNotify ?? emailToNotify,
                        emailToNotify,
                        defaultPasswordToEmail
                    );
                }
                catch (Exception ex)
                {
                    emailSent = false;
                    _logger.LogError(ex, $"Tạo tài khoản Google thành công ({user.UserId}) nhưng gửi email thất bại.");
                }
            }

            // 6) Generate JWT (lấy thời gian hết hạn từ SystemConfig)
            int tokenExpirationMinutes = await GetIntConfigAsync("TOKEN_EXPIRATION_MINUTES", 60);
            DateTime tokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes);

            var token = _jwtGenerator.GenerateToken(user, tokenExpiresAtUtc);


            string successMessage = isNewAccount
                ? (emailSent
                    ? "Đăng ký tài khoản Google thành công. Vui lòng kiểm tra email để lấy thông tin đăng nhập."
                    : "Đăng ký tài khoản Google thành công nhưng gửi email thất bại. Vui lòng dùng chức năng Quên mật khẩu để đặt lại mật khẩu.")
                : "Đăng nhập Google thành công";

            return OperationResult<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }, 200, successMessage);
        }

        private OperationResult<LoginResponse>? CheckAccountStatus(Account user, DateTime nowLocal)
        {
            if (user.Status == AccountStatus.Inactive)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.AccountInActive },
                    403,
                    "Tài khoản của bạn không hoạt động.");
            }

            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.AccountBanned },
                    403,
                    "Tài khoản của bạn đã bị khóa vĩnh viễn.");
            }

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
            {
                var remainingMinutes = (int)Math.Ceiling((user.LockedUntil.Value - nowLocal).TotalMinutes);
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.AccountLocked },
                    403,
                    $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
            }

            return null;
        }
        private async Task<int> GetIntConfigAsync(string key, int defaultValue)
        {
            var val = await _systemConfigRepository.GetValueByKeyAsync(key);
            if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out var result) && result > 0)
            {
                return result;
            }
            return defaultValue;
        }

    }
}
