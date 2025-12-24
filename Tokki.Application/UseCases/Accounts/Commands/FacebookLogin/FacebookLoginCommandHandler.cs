using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IEmailService _emailService;
        private readonly FacebookAuthSettings _facebookSettings;
        private readonly ILogger<FacebookLoginCommandHandler> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FacebookLoginCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IEmailService emailService,
            IOptions<FacebookAuthSettings> facebookOptions,
            ILogger<FacebookLoginCommandHandler> logger)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _emailService = emailService;
            _facebookSettings = facebookOptions.Value;
            _logger = logger;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            FacebookLoginCommand request,
            CancellationToken cancellationToken)
        {
            var nowLocal = DateTime.UtcNow.AddHours(7);

            // 1) Validate token + lấy userData (id luôn phải có; email/birthday/gender có thể null)
            FacebookUserData? userData;
            try
            {
                userData = await ValidateFacebookTokenAsync(request.AccessToken, cancellationToken);

                if (userData == null || string.IsNullOrEmpty(userData.Id))
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.InvalidFacebookToken },
                        401,
                        "Facebook token không hợp lệ");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Facebook authentication failed.");
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    401,
                    "Facebook authentication failed");
            }

            // 2) Ưu tiên check liên kết theo Facebook ID trước (KHÔNG cần email)
            var socialLogin = await _socialLoginRepo.GetByProviderAsync("facebook", userData.Id);

            Account user;
            bool isNewAccount = false;

            // Dùng để gửi email SAU KHI SaveChanges thành công
            string? defaultPasswordToEmail = null;
            string? emailToNotify = null;
            string? nameToNotify = null;

            if (socialLogin != null)
            {
                // Đã có SocialLogin -> login bình thường (không phụ thuộc email từ Facebook)
                user = await _accountRepo.GetByIdAsync(socialLogin.UserId);

                if (user == null)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.UserNotFoundById },
                        404,
                        "Tài khoản không tồn tại.");
                }

                // Kiểm tra tài khoản có bị khóa không
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
                {
                    var remainingMinutes = (int)(user.LockedUntil.Value - nowLocal).TotalMinutes;
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountLocked },
                        403,
                        $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                }

                // Kiểm tra trạng thái tài khoản
                if (user.Status == AccountStatus.Inactive)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountInActive },
                        403,
                        "Tài khoản của bạn không hoạt động.");
                }

               
            }
            else
            {
                // 3) Chưa có SocialLogin
                // Nếu thiếu email -> trả về info để bạn chạy flow "đăng ký tài khoản Facebook" (bổ sung email)
                if (string.IsNullOrWhiteSpace(userData.Email))
                {
                    var preRegisterResponse = new LoginResponse
                    {
                        Token = string.Empty, // chưa phát JWT
                        FullName = userData.Name ?? string.Empty,
                        Role = string.Empty,
                        AvatarUrl = null,

                        // Thông tin bạn cần để làm hàm đăng ký
                        FacebookId = userData.Id,
                        Name = userData.Name ?? string.Empty,
                        Birthday = userData.Birthday ?? string.Empty,
                        Gender = userData.Gender ?? string.Empty,

                        RequireFacebookRegister = true
                    };

                    return OperationResult<LoginResponse>.Success(
                        preRegisterResponse,
                        200,
                        "Không lấy được email từ Facebook. Vui lòng đăng ký/bổ sung email để hoàn tất.");
                }

                // Có email -> xử lý tạo mới / merge như cũ
                user = await _accountRepo.GetByEmailAsync(userData.Email);

                if (user == null)
                {
                    // Email chưa tồn tại -> tạo account mới với mật khẩu mặc định
                    string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER");

                    if (string.IsNullOrEmpty(defaultPassword))
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
                            Email = userData.Email,
                            FullName = userData.Name ?? userData.Email,
                            PasswordHash = passwordHash,
                            Role = AccountRole.User,
                            AvatarUrl = null, // không lấy từ Facebook nữa
                            Status = AccountStatus.Active,
                            CreatedAt = nowLocal,
                            UpdatedAt = nowLocal
                        };

                        await _accountRepo.AddAsync(user);

                        await _socialLoginRepo.AddAsync(new SocialLogin
                        {
                            Id = _idGenerator.Generate(15),
                            UserId = user.UserId,
                            Provider = "facebook",
                            ProviderUserId = userData.Id,
                            EmailFromProvider = userData.Email,
                            NameFromProvider = userData.Name
                        });

                        isNewAccount = true;

                        // Ghi nhận để gửi email SAU KHI SaveChanges thành công
                        defaultPasswordToEmail = defaultPassword;
                        emailToNotify = userData.Email;
                        nameToNotify = userData.Name ?? userData.Email;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo tài khoản Facebook.");
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.ServerError },
                            500,
                            ex.Message);
                    }
                }
                else
                {
                    // Email đã tồn tại -> kiểm tra tài khoản có bị khóa không
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
                    {
                        var remainingMinutes = (int)(user.LockedUntil.Value - nowLocal).TotalMinutes;
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.AccountLocked },
                            403,
                            $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                    }

                    // Kiểm tra trạng thái tài khoản
                    if (user.Status == AccountStatus.Inactive)
                    {
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.AccountInActive },
                            403,
                            "Tài khoản của bạn không hoạt động.");
                    }

                    // Email đã tồn tại -> cần xác nhận merge
                    if (!request.IsComfirmToMergeAcc)
                    {
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> { AppErrors.MergeAccountRequered },
                            409,
                            "Account merge confirmation required");
                    }

                    // Người dùng đã xác nhận merge -> tạo SocialLogin
                    await _socialLoginRepo.AddAsync(new SocialLogin
                    {
                        Id = _idGenerator.Generate(15),
                        UserId = user.UserId,
                        Provider = "facebook",
                        ProviderUserId = userData.Id,
                        EmailFromProvider = userData.Email,
                        NameFromProvider = userData.Name
                    });
                }
            }

            // 4) Cập nhật LastLoginAt
            user.LastLoginAt = nowLocal;
            user.UpdatedAt = nowLocal;

            await _accountRepo.UpdateUserAsync(user);

            // Commit DB trước khi gửi email (nếu có)
            await _accountRepo.SaveChangesAsync(cancellationToken);

            // 5) Gửi Email thông báo tài khoản mới (CHỈ sau khi SaveChanges thành công)
            if (isNewAccount &&
                !string.IsNullOrEmpty(defaultPasswordToEmail) &&
                !string.IsNullOrEmpty(emailToNotify))
            {
                try
                {
                    await _emailService.SendFacebookAccountInfoAsync(
                        emailToNotify,
                        nameToNotify ?? emailToNotify,
                        emailToNotify,
                        defaultPasswordToEmail
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Tạo tài khoản Facebook thành công ({user.UserId}) nhưng gửi email thất bại.");
                }
            }

            // 6) Generate JWT token
            var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

            string successMessage = isNewAccount
                ? "Đăng ký tài khoản Facebook thành công. Vui lòng kiểm tra email để lấy thông tin đăng nhập."
                : "Đăng nhập Facebook thành công";

            return OperationResult<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl,

                RequireFacebookRegister = false,
                FacebookId = string.Empty,
                Name = string.Empty,
                Birthday = string.Empty,
                Gender = string.Empty
            }, 200, successMessage);
        }

        private async Task<FacebookUserData?> ValidateFacebookTokenAsync(string accessToken, CancellationToken cancellationToken)
        {
            // Encode để tránh lỗi query string với token có ký tự đặc biệt
            var inputToken = Uri.EscapeDataString(accessToken);
            var appAccessToken = Uri.EscapeDataString($"{_facebookSettings.AppId}|{_facebookSettings.AppSecret}");

            // 1) Verify token với debug_token
            var verifyUrl =
                $"https://graph.facebook.com/debug_token?input_token={inputToken}&access_token={appAccessToken}";

            var verifyResponse = await _httpClient.GetAsync(verifyUrl, cancellationToken);

            if (!verifyResponse.IsSuccessStatusCode)
                throw new Exception("Failed to verify Facebook token (debug_token).");

            var verifyJson = await verifyResponse.Content.ReadAsStringAsync(cancellationToken);
            var debugToken = JsonSerializer.Deserialize<FacebookDebugTokenResponse>(
                verifyJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Token invalid hoặc thiếu data
            if (debugToken?.Data == null || !debugToken.Data.IsValid)
                return null;

            // Token phải thuộc đúng App của bạn
            if (!string.Equals(debugToken.Data.AppId, _facebookSettings.AppId, StringComparison.Ordinal))
                return null;

            // 2) Get user data từ Facebook (/me) - KHÔNG lấy picture nữa
            var userDataUrl =
                $"https://graph.facebook.com/me?fields=id,email,name,birthday,gender&access_token={inputToken}";

            var userDataResponse = await _httpClient.GetAsync(userDataUrl, cancellationToken);

            if (!userDataResponse.IsSuccessStatusCode)
                throw new Exception("Failed to get user data from Facebook (/me).");

            var json = await userDataResponse.Content.ReadAsStringAsync(cancellationToken);
            var userData = JsonSerializer.Deserialize<FacebookUserData>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userData == null || string.IsNullOrEmpty(userData.Id))
                return null;

            // Ensure user_id từ debug_token khớp id từ /me (tăng độ chắc chắn)
            if (!string.IsNullOrEmpty(debugToken.Data.UserId) &&
                !string.Equals(debugToken.Data.UserId, userData.Id, StringComparison.Ordinal))
            {
                return null;
            }

            return userData;
        }

        private sealed class FacebookDebugTokenResponse
        {
            [JsonPropertyName("data")]
            public FacebookDebugTokenData? Data { get; set; }
        }

        private sealed class FacebookDebugTokenData
        {
            [JsonPropertyName("app_id")]
            public string? AppId { get; set; }

            [JsonPropertyName("is_valid")]
            public bool IsValid { get; set; }

            [JsonPropertyName("user_id")]
            public string? UserId { get; set; }

            [JsonPropertyName("expires_at")]
            public long? ExpiresAt { get; set; }
        }
    }
}
