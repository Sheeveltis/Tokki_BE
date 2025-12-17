using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookCompleteRegistrationCommandHandler
        : IRequestHandler<FacebookCompleteRegistrationCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;

        public FacebookCompleteRegistrationCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            FacebookCompleteRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            // Validate email
            if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains("@"))
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> {
                        new Error("INVALID_EMAIL", "Email không hợp lệ")
                    },
                    400,
                    "Invalid email");
            }

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _accountRepo.GetByEmailAsync(request.Email);

            if (existingUser != null)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> {
                        new Error("EMAIL_ALREADY_EXISTS", "Email này đã được sử dụng")
                    },
                    409,
                    "Email already exists");
            }

            // Kiểm tra Facebook ID đã được đăng ký chưa
            var existingSocialLogin = await _socialLoginRepo
                .GetByProviderAsync("facebook", request.FacebookId);

            if (existingSocialLogin != null)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> {
                        new Error("FACEBOOK_ALREADY_LINKED", "Tài khoản Facebook này đã được liên kết")
                    },
                    409,
                    "Facebook account already linked");
            }

            // Tạo account mới
            var user = new Account
            {
                UserId = _idGenerator.Generate(15),
                Email = request.Email,
                FullName = request.Name,
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _accountRepo.AddAsync(user);

            // Tạo SocialLogin
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

            // Generate JWT token
            var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

            return OperationResult<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }, 200, "Đăng ký thành công");
        }
    }
}
