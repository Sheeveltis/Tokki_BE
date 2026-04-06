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
    public class FacebookCompleteRegistrationCommandHandler
        : IRequestHandler<FacebookCompleteRegistrationCommand, OperationResult<FacebookLoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IEmailService _emailService;
        private readonly FacebookAuthSettings _facebookSettings;
        private readonly ILogger<FacebookCompleteRegistrationCommandHandler> _logger;

        public static HttpClient _httpClient = new HttpClient();

        public FacebookCompleteRegistrationCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IEmailService emailService,
            IOptions<FacebookAuthSettings> facebookOptions,
            ILogger<FacebookCompleteRegistrationCommandHandler> logger)
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

        public async Task<OperationResult<FacebookLoginResponse>> Handle(
            FacebookCompleteRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            var nowLocal = DateTime.UtcNow.AddHours(7);

            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    401,
                    "Facebook token không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(request.FacebookId))
            {
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    400,
                    "FacebookId không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.FacebookEmailRequired },
                    400,
                    "Email là bắt buộc để hoàn tất đăng ký.");
            }

            // 0) VERIFY TOKEN (ANTI-BYPASS)
            FacebookUserData? userData;
            try
            {
                userData = await ValidateFacebookTokenAsync(request.AccessToken, cancellationToken);

                if (userData == null || string.IsNullOrWhiteSpace(userData.Id))
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.InvalidFacebookToken },
                        401,
                        "Facebook token không hợp lệ.");
                }

                if (!string.Equals(userData.Id, request.FacebookId, StringComparison.Ordinal))
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.FacebookIdMismatch },
                        401,
                        AppErrors.FacebookIdMismatch.Description
                    );
                }

                if (!string.IsNullOrWhiteSpace(userData.Email) &&
                    !string.Equals(userData.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.FacebookEmailMismatch },
                        400,
                        AppErrors.FacebookEmailMismatch.Description
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Facebook token verification failed in complete registration.");
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    401,
                    "Facebook token không hợp lệ.");
            }

            // 1) FacebookId already linked?
            var existingSocialLogin = await _socialLoginRepo.GetByProviderAsync("facebook", request.FacebookId);
            if (existingSocialLogin != null)
            {
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.MergeAccountRequered },
                    409,
                    "Tài khoản Facebook này đã được liên kết.");
            }

            // 2) Email existed?
            var existingUser = await _accountRepo.GetByEmailAsync(request.Email);

            // CASE A: Email exists -> MERGE
            if (existingUser != null)
            {
                if (existingUser.LockedUntil.HasValue && existingUser.LockedUntil.Value > nowLocal)
                {
                    var remainingMinutes = (int)Math.Ceiling((existingUser.LockedUntil.Value - nowLocal).TotalMinutes);
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountLocked },
                        403,
                        $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                }

                if (existingUser.Status == AccountStatus.Inactive)
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountInActive },
                        403,
                        "Tài khoản của bạn không hoạt động.");
                }

                if (!request.IsComfirmToMergeAcc)
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.MergeAccountRequered },
                        409,
                        "Account merge confirmation required");
                }

                await _socialLoginRepo.AddAsync(new SocialLogin
                {
                    Id = _idGenerator.Generate(15),
                    UserId = existingUser.UserId,
                    Provider = "facebook",
                    ProviderUserId = request.FacebookId,
                    EmailFromProvider = request.Email,
                    NameFromProvider = request.Name
                });

                existingUser.LastLoginAt = nowLocal;
                existingUser.UpdatedAt = nowLocal;
                await _accountRepo.UpdateUserAsync(existingUser);

                await _accountRepo.SaveChangesAsync(cancellationToken);

                var tokenMerged = _jwtGenerator.GenerateToken(existingUser, DateTime.UtcNow.AddMinutes(60));

                return OperationResult<FacebookLoginResponse>.Success(new FacebookLoginResponse
                {
                    Token = tokenMerged,
                    FullName = existingUser.FullName,
                    Role = existingUser.Role.ToString(),
                    AvatarUrl = existingUser.AvatarUrl,

                    RequireFacebookRegister = false,
                    FacebookId = request.FacebookId,
                }, 200, "Liên kết Facebook thành công");
            }

            // CASE B: Email not exists -> CREATE NEW
            string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER");
            if (string.IsNullOrEmpty(defaultPassword))
            {
                _logger.LogError("Cấu hình DEFAULT_PASSWORD_FOR_USER không được tìm thấy hoặc rỗng.");
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    "Cấu hình mật khẩu mặc định cho User chưa được thiết lập.");
            }

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                var user = new Account
                {
                    UserId = _idGenerator.Generate(15),
                    Email = request.Email,
                    FullName = string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name!,
                    PasswordHash = passwordHash,
                    Role = AccountRole.User,
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
                    Provider = "facebook",
                    ProviderUserId = request.FacebookId,
                    EmailFromProvider = request.Email,
                    NameFromProvider = request.Name
                });

                await _accountRepo.SaveChangesAsync(cancellationToken);

                bool emailSent = true;
                try
                {
                    await _emailService.SendFacebookAccountInfoAsync(
                        request.Email,
                        string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name!,
                        request.Email,
                        defaultPassword
                    );
                }
                catch (Exception ex)
                {
                    emailSent = false;
                    _logger.LogError(ex, $"Đăng ký tài khoản Facebook thành công ({user.UserId}) nhưng gửi email thất bại.");
                }

                var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

                var message = emailSent
                    ? "Đăng ký thành công. Vui lòng kiểm tra email để lấy thông tin đăng nhập."
                    : "Đăng ký thành công nhưng gửi email thất bại. Vui lòng dùng chức năng Quên mật khẩu để đặt lại mật khẩu.";

                return OperationResult<FacebookLoginResponse>.Success(new FacebookLoginResponse
                {
                    Token = token,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    AvatarUrl = user.AvatarUrl,

                    RequireFacebookRegister = false,
                    FacebookId = request.FacebookId,
                }, 200, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình đăng ký tài khoản Facebook.");
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message);
            }
        }

        private async Task<FacebookUserData?> ValidateFacebookTokenAsync(string accessToken, CancellationToken cancellationToken)
        {
            var inputToken = Uri.EscapeDataString(accessToken);
            var appAccessToken = Uri.EscapeDataString($"{_facebookSettings.AppId}|{_facebookSettings.AppSecret}");

            var verifyUrl =
                $"https://graph.facebook.com/debug_token?input_token={inputToken}&access_token={appAccessToken}";

            var verifyResponse = await _httpClient.GetAsync(verifyUrl, cancellationToken);
            if (!verifyResponse.IsSuccessStatusCode)
                throw new Exception("Failed to verify Facebook token (debug_token).");

            var verifyJson = await verifyResponse.Content.ReadAsStringAsync(cancellationToken);
            var debugToken = JsonSerializer.Deserialize<FacebookDebugTokenResponse>(
                verifyJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (debugToken?.Data == null || !debugToken.Data.IsValid)
                return null;

            if (!string.Equals(debugToken.Data.AppId, _facebookSettings.AppId, StringComparison.Ordinal))
                return null;

            var userDataUrl =
                $"https://graph.facebook.com/me?fields=id,email,name&access_token={inputToken}";

            var userDataResponse = await _httpClient.GetAsync(userDataUrl, cancellationToken);
            if (!userDataResponse.IsSuccessStatusCode)
                return null;

            var json = await userDataResponse.Content.ReadAsStringAsync(cancellationToken);
            var userData = JsonSerializer.Deserialize<FacebookUserData>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userData == null || string.IsNullOrEmpty(userData.Id))
                return null;

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
