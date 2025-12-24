using System;
using System.Collections.Generic;
using System.Threading;
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
    public class FacebookCompleteRegistrationCommandHandler
        : IRequestHandler<FacebookCompleteRegistrationCommand, OperationResult<LoginResponse>>
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
            var nowLocal = DateTime.UtcNow.AddHours(7);

            if (string.IsNullOrWhiteSpace(request.FacebookId))
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.InvalidFacebookToken },
                    400,
                    "FacebookId không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.FacebookEmailRequired },
                    400,
                    "Email là bắt buộc để hoàn tất đăng ký.");
            }

            // 1) FacebookId đã được liên kết chưa?
            var existingSocialLogin = await _socialLoginRepo.GetByProviderAsync("facebook", request.FacebookId);
            if (existingSocialLogin != null)
            {
                return OperationResult<LoginResponse>.Failure(
                    new List<Error> { AppErrors.MergeAccountRequered }, // nếu bạn có error riêng thì thay vào
                    409,
                    "Tài khoản Facebook này đã được liên kết.");
            }

            // 2) Email đã tồn tại trong hệ thống chưa?
            var existingUser = await _accountRepo.GetByEmailAsync(request.Email);

            // CASE A: Email đã tồn tại -> MERGE (nếu user confirm)
            if (existingUser != null)
            {
                // Check lock/status giống flow login
                if (existingUser.LockedUntil.HasValue && existingUser.LockedUntil.Value > nowLocal)
                {
                    var remainingMinutes = (int)(existingUser.LockedUntil.Value - nowLocal).TotalMinutes;
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountLocked },
                        403,
                        $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
                }

                if (existingUser.Status == AccountStatus.Inactive)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.AccountInActive },
                        403,
                        "Tài khoản của bạn không hoạt động.");
                }

                if (!request.IsComfirmToMergeAcc)
                {
                    return OperationResult<LoginResponse>.Failure(
                        new List<Error> { AppErrors.MergeAccountRequered },
                        409,
                        "Account merge confirmation required");
                }

                // Tạo SocialLogin trỏ vào account hiện có
                await _socialLoginRepo.AddAsync(new SocialLogin
                {
                    Id = _idGenerator.Generate(15),
                    UserId = existingUser.UserId,
                    Provider = "facebook",
                    ProviderUserId = request.FacebookId,
                    EmailFromProvider = request.Email,
                    NameFromProvider = request.Name
                });

                // Update last login
                existingUser.LastLoginAt = nowLocal;
                existingUser.UpdatedAt = nowLocal;
                await _accountRepo.UpdateUserAsync(existingUser);

                await _accountRepo.SaveChangesAsync(cancellationToken);

                var tokenMerged = _jwtGenerator.GenerateToken(existingUser, DateTime.UtcNow.AddMinutes(60));

                return OperationResult<LoginResponse>.Success(new LoginResponse
                {
                    Token = tokenMerged,
                    FullName = existingUser.FullName,              // giữ tên trong hệ thống
                    Role = existingUser.Role.ToString(),
                    AvatarUrl = existingUser.AvatarUrl,

                    RequireFacebookRegister = false,
                    FacebookId = request.FacebookId,
                    Name = request.Name,
                    Birthday = request.Birthday ?? string.Empty,
                    Gender = request.Gender ?? string.Empty
                }, 200, "Liên kết Facebook thành công");
            }

            // CASE B: Email chưa tồn tại -> TẠO MỚI
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

                // Nếu bạn muốn "chỉ lấy tên trong hệ thống", bạn có thể set FullName = request.Email
                // hoặc một rule khác. Hiện tại để request.Name cho tiện.
                var user = new Account
                {
                    UserId = _idGenerator.Generate(15),
                    Email = request.Email,
                    FullName = string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name,
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

                // gửi email mật khẩu mặc định (như bạn đang làm)
                try
                {
                    await _emailService.SendFacebookAccountInfoAsync(
                        request.Email,
                        string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name,
                        request.Email,
                        defaultPassword
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Đăng ký tài khoản Facebook thành công ({user.UserId}) nhưng gửi email thất bại.");
                }

                var token = _jwtGenerator.GenerateToken(user, DateTime.UtcNow.AddMinutes(60));

                return OperationResult<LoginResponse>.Success(new LoginResponse
                {
                    Token = token,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    AvatarUrl = user.AvatarUrl,

                    RequireFacebookRegister = false,
                    FacebookId = request.FacebookId,
                    Name = request.Name,
                    Birthday = request.Birthday ?? string.Empty,
                    Gender = request.Gender ?? string.Empty
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
