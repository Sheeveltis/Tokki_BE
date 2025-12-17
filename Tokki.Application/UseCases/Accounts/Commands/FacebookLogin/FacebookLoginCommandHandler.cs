using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Http;
using MediatR;
using Microsoft.Extensions.Options;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly FacebookAuthSettings _facebookSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FacebookLoginCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IOptions<FacebookAuthSettings> facebookOptions)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _facebookSettings = facebookOptions.Value;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            FacebookLoginCommand request,
            CancellationToken cancellationToken)
        {
            // Validate Facebook Access Token
            FacebookUserData? userData;
            try
            {
                userData = await ValidateFacebookTokenAsync(request.AccessToken);

                if (userData == null || string.IsNullOrEmpty(userData.Email))
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> {
                            new Error("INVALID_FACEBOOK_TOKEN", "Facebook token không hợp lệ hoặc không có quyền truy cập email")
                        },
                        401,
                        "Facebook authentication failed");
                }
            }
            catch
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> {
                        new Error("INVALID_FACEBOOK_TOKEN", "Facebook token không hợp lệ")
                    },
                    401,
                    "Facebook authentication failed");
            }

            var socialLogin = await _socialLoginRepo
                .GetByProviderAsync("facebook", userData.Id);

            Account user;

            if (socialLogin != null)
            {
                // Đã có SocialLogin -> login bình thường
                user = await _accountRepo.GetByIdAsync(socialLogin.UserId);
            }
            else
            {
                // Chưa có SocialLogin -> kiểm tra xem email đã tồn tại chưa
                user = await _accountRepo.GetByEmailAsync(userData.Email);

                if (user == null)
                {
                    // Email chưa tồn tại -> tạo account mới
                    user = new Account
                    {
                        UserId = _idGenerator.Generate(15),
                        Email = userData.Email,
                        FullName = userData.Name ?? userData.Email,
                        AvatarUrl = userData.Picture?.Data?.Url,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };

                    await _accountRepo.AddAsync(user);

                    // Tạo SocialLogin cho account mới
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
                else
                {
                    // Email đã tồn tại -> cần xác nhận merge
                    if (!request.IsComfirmToMergeAcc)
                    {
                        // Người dùng chưa xác nhận merge
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> {
                                new Error("MERGE_ACCOUNT_REQUIRED",
                                    "Email này đã được đăng ký. Bạn có muốn kết nối tài khoản Facebook với tài khoản hiện có không?")
                            },
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

            await _accountRepo.SaveChangesAsync(cancellationToken);

            // Generate JWT token
            var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

            return OperationResult<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }, 200, "Login Facebook thành công");
        }

        private async Task<FacebookUserData?> ValidateFacebookTokenAsync(string accessToken)
        {
            // Verify token với Facebook Graph API
            var verifyUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={_facebookSettings.AppId}|{_facebookSettings.AppSecret}";
            var verifyResponse = await _httpClient.GetAsync(verifyUrl);

            if (!verifyResponse.IsSuccessStatusCode)
            {
                throw new Exception("Invalid Facebook token");
            }

            // Get user data từ Facebook
            var userDataUrl = $"https://graph.facebook.com/me?fields=id,email,name,picture.type(large)&access_token={accessToken}";
            var userDataResponse = await _httpClient.GetAsync(userDataUrl);

            if (!userDataResponse.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get user data from Facebook");
            }

            var json = await userDataResponse.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<FacebookUserData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return userData;
        }
    }

}
