using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookCompleteRegistrationCommandHandler : IRequestHandler<FacebookCompleteRegistrationCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ISocialLoginRepository _socialLoginRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IEmailService _emailService;
        private readonly ILogger<FacebookCompleteRegistrationCommandHandler> _logger;

        public FacebookCompleteRegistrationCommandHandler(
            IAccountRepository accountRepo,
            ISocialLoginRepository socialLoginRepo,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IEmailService emailService,
            ILogger<FacebookCompleteRegistrationCommandHandler> logger)
        {
            _accountRepo = accountRepo;
            _socialLoginRepo = socialLoginRepo;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<LoginResponse>> Handle(
            FacebookCompleteRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _accountRepo.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.EmailDuplicated },
                    409,
                    "Email này đã được sử dụng");
            }

            // Kiểm tra Facebook ID đã được đăng ký chưa
            var existingSocialLogin = await _socialLoginRepo
                .GetByProviderAsync("facebook", request.FacebookId);
            if (existingSocialLogin != null)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.EmailDuplicated },
                    409,
                    "Tài khoản Facebook này đã được liên kết");
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

            // Lấy Password mặc định từ SystemConfig
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
                // Hash Password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                // Tạo account mới
                var user = new Account
                {
                    UserId = _idGenerator.Generate(15),
                    Email = request.Email,
                    FullName = request.Name,
                    PasswordHash = passwordHash,
                    Role = AccountRole.User,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
                    LastLoginAt = DateTime.UtcNow.AddHours(7)
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

                // Gửi Email thông báo tài khoản
                try
                {
                    await _emailService.SendFacebookAccountInfoAsync(
                        request.Email,
                        request.Name,
                        request.Email,
                        defaultPassword
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Đăng ký tài khoản Facebook thành công ({user.UserId}) nhưng gửi email thất bại.");
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình đăng ký tài khoản Facebook.");
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message);
            }
        }
    }
}