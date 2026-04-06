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
    public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommand, OperationResult<FacebookLoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IEmailService _emailService;
        private readonly FacebookAuthSettings _facebookSettings;
        private readonly ILogger<FacebookLoginCommandHandler> _logger;
        public static HttpClient _httpClient = new HttpClient();

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

        public async Task<OperationResult<FacebookLoginResponse>> Handle(
            FacebookLoginCommand request,
            CancellationToken cancellationToken)
        {
            var nowLocal = DateTime.UtcNow.AddHours(7);

            FacebookUserData? userData;
            try
            {
                userData = await ValidateFacebookTokenAsync(request.AccessToken, cancellationToken);

                if (userData == null || string.IsNullOrEmpty(userData.Id))
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.InvalidFacebookToken },
                        401,
                        "Facebook token không hợp lệ");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Facebook authentication failed.");
                return OperationResult<FacebookLoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    401,
                    "Facebook authentication failed");
            }

            var socialLogin = await _socialLoginRepo.GetByProviderAsync("facebook", userData.Id);

            Account user;
            bool isNewAccount = false;

            string? defaultPasswordToEmail = null;
            string? emailToNotify = null;
            string? nameToNotify = null;

            if (socialLogin != null)
            {
                user = await _accountRepo.GetByIdAsync(socialLogin.UserId);

                if (user == null)
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.UserNotFoundById },
                        404,
                        "Tài khoản không tồn tại.");
                }

                if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
                {
                    var remainingMinutes = (int)(user.LockedUntil.Value - nowLocal).TotalMinutes;
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountLocked },
                        403,
                        $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                }

                if (user.Status == AccountStatus.Inactive)
                {
                    return OperationResult<FacebookLoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountInActive },
                        403,
                        "Tài khoản của bạn không hoạt động.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userData.Email))
                {
                    var preRegisterResponse = new FacebookLoginResponse
                    {
                        Token = string.Empty,
                        FullName = userData.Name ?? string.Empty,
                        Role = string.Empty,
                        AvatarUrl = null,

                        RequireFacebookRegister = true,
                        FacebookId = userData.Id,
                    };

                    return OperationResult<FacebookLoginResponse>.Success(
                        preRegisterResponse,
                        200,
                        "Không lấy được email từ Facebook. Vui lòng đăng ký/bổ sung email để hoàn tất.");
                }

                user = await _accountRepo.GetByEmailAsync(userData.Email);

                if (user == null)
                {
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

                        user = new Account
                        {
                            UserId = _idGenerator.Generate(15),
                            Email = userData.Email,
                            FullName = userData.Name ?? userData.Email,
                            PasswordHash = passwordHash,
                            Role = AccountRole.User,
                            AvatarUrl = null,
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

                        defaultPasswordToEmail = defaultPassword;
                        emailToNotify = userData.Email;
                        nameToNotify = userData.Name ?? userData.Email;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo tài khoản Facebook.");
                        return OperationResult<FacebookLoginResponse>.Failure(
                            new List<Error> { AppErrors.ServerError },
                            500,
                            ex.Message);
                    }
                }
                else
                {
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
                    {
                        var remainingMinutes = (int)(user.LockedUntil.Value - nowLocal).TotalMinutes;
                        return OperationResult<FacebookLoginResponse>.Failure(
                            new List<Error> { AppErrors.AccountLocked },
                            403,
                            $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                    }
                    if (user.Status == AccountStatus.Banned)
                    {
                        return OperationResult<FacebookLoginResponse>.Failure(new List<Error> { AppErrors.AccountBanned }, 403, "Tài khoản của bạn đã bị khóa vĩnh viễn.");
                    }

                    if (user.Status == AccountStatus.Inactive)
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
                        UserId = user.UserId,
                        Provider = "facebook",
                        ProviderUserId = userData.Id,
                        EmailFromProvider = userData.Email,
                        NameFromProvider = userData.Name
                    });
                }
            }

            user.LastLoginAt = nowLocal;
            user.UpdatedAt = nowLocal;

            await _accountRepo.UpdateUserAsync(user);
            await _accountRepo.SaveChangesAsync(cancellationToken);

            bool emailSent = true;

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
                    emailSent = false;
                    _logger.LogError(ex, $"Created Facebook account ({user.UserId}) but email sending failed.");
                }
            }
            // 6) Generate JWT (lấy thời gian hết hạn từ SystemConfig)
            int tokenExpirationMinutes = await GetIntConfigAsync("TOKEN_EXPIRATION_MINUTES", 60);
            DateTime tokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes);

            var token = _jwtGenerator.GenerateToken(user, tokenExpiresAtUtc);


            var successMessage = isNewAccount
                ? (emailSent
                    ? "Đăng ký tài khoản Facebook thành công. Vui lòng kiểm tra email để lấy thông tin đăng nhập."
                    : "Đăng ký tài khoản Facebook thành công nhưng gửi email thất bại. Vui lòng dùng chức năng Quên mật khẩu để đặt lại mật khẩu.")
                : "Đăng nhập Facebook thành công";

            return OperationResult<FacebookLoginResponse>.Success(new FacebookLoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl,
                RequireFacebookRegister = false,
                FacebookId = string.Empty,
            }, 200, successMessage);
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
                throw new Exception("Failed to get user data from Facebook (/me).");

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
