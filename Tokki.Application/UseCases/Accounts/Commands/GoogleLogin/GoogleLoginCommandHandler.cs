using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using MediatR;
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
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly GoogleAuthSettings _googleSettings;

        public GoogleLoginCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IOptions<GoogleAuthSettings> googleOptions)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _googleSettings = googleOptions.Value;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            GoogleLoginCommand request,
            CancellationToken cancellationToken)
        {
            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = _googleSettings.ClientIds
                    });
            }
            catch
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> {
                        new Error("INVALID_GOOGLE_TOKEN", "Google token không hợp lệ")
                    },
                    401,
                    "Google authentication failed");
            }

            var socialLogin = await _socialLoginRepo
                .GetByProviderAsync("google", payload.Subject);

            Account user;

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

                // ✅ Kiểm tra tài khoản có bị khóa không
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow.AddHours(7))
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> {
                            new Error("Account.Locked", $"Tài khoản bị khóa đến {user.LockedUntil.Value:dd/MM/yyyy HH:mm}")
                        },
                        403,
                        "Tài khoản của bạn đang bị khóa.");
                }

                // ✅ Kiểm tra trạng thái tài khoản
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
                // Chưa có SocialLogin -> kiểm tra xem email đã tồn tại chưa
                user = await _accountRepo.GetByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Email chưa tồn tại -> tạo account mới
                    user = new Account
                    {
                        UserId = _idGenerator.Generate(15),
                        Email = payload.Email,
                        FullName = payload.Name ?? payload.Email,
                        AvatarUrl = payload.Picture,
                        Status = AccountStatus.Active,
                        CreatedAt = DateTime.UtcNow.AddHours(7),
                        UpdatedAt = DateTime.UtcNow.AddHours(7)
                    };

                    await _accountRepo.AddAsync(user);

                    // Tạo SocialLogin cho account mới
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
                else
                {
                    // ✅ Email đã tồn tại -> kiểm tra tài khoản có bị khóa không
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow.AddHours(7))
                    {
                        return OperationResult<LoginResponse>.Failure(
                            new List<Error> {
                                new Error("Account.Locked", $"Tài khoản bị khóa đến {user.LockedUntil.Value:dd/MM/yyyy HH:mm}")
                            },
                            403,
                            "Tài khoản của bạn đang bị khóa.");
                    }

                    // ✅ Kiểm tra trạng thái tài khoản
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
                        // Người dùng chưa xác nhận merge
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
                        Provider = "google",
                        ProviderUserId = payload.Subject,
                        EmailFromProvider = payload.Email,
                        NameFromProvider = payload.Name
                    });
                }
            }

            // ✅ Cập nhật LastLoginAt
            user.LastLoginAt = DateTime.UtcNow.AddHours(7);
            await _accountRepo.UpdateUserAsync(user);
            await _accountRepo.SaveChangesAsync(cancellationToken);

            // Generate JWT token
            var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

            return OperationResult<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }, 200, "Login Google thành công");
        }
    }
}