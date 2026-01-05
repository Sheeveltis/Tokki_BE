using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaff
{
    public class CreateAccountByAdminCommandHandler : IRequestHandler<CreateAccountByAdminCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateAccountByAdminCommandHandler> _logger;

        public CreateAccountByAdminCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IIdGeneratorService idGeneratorService,
            IEmailService emailService,
            ILogger<CreateAccountByAdminCommandHandler> logger)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _idGeneratorService = idGeneratorService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateAccountByAdminCommand request, CancellationToken cancellationToken)
        {
   

            // 1. Kiểm tra Email tồn tại
            if (await _accountRepository.IsEmailExistsAsync(request.Email))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.EmailDuplicated },
                    409,
                    "Email đã tồn tại."
                );
            }

            // 2. Kiểm tra Phone tồn tại (chỉ khi có số điện thoại)
            if (!string.IsNullOrEmpty(request.PhoneNumber) && await _accountRepository.IsPhoneNumberExistsAsync(request.PhoneNumber))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.PhoneNumberDuplicated },
                    409,
                    "Số điện thoại đã tồn tại."
                );
            }

            // 3. Lấy Password mặc định từ SystemConfig dựa theo Role
            string configKey = request.Role switch
            {
                AccountRole.Admin => "DEFAULT_PASSWORD_FOR_ADMIN",
                AccountRole.Staff => "DEFAULT_PASSWORD_FOR_STAFF",
                AccountRole.Moderator => "DEFAULT_PASSWORD_FOR_STAFF",
                AccountRole.User => "DEFAULT_PASSWORD_FOR_USER",
                _ => "DEFAULT_PASSWORD_FOR_STAFF"
            };

            string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync(configKey);

            // Fallback: nếu không tìm thấy config cho role cụ thể, thử dùng config chung
            if (string.IsNullOrEmpty(defaultPassword))
            {
                _logger.LogWarning($"Không tìm thấy cấu hình {configKey}, thử sử dụng DEFAULT_PASSWORD_FOR_STAFF.");
                defaultPassword = await _systemConfigRepository.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF");
            }

            if (string.IsNullOrEmpty(defaultPassword))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Cấu hình mật khẩu mặc định cho {request.Role} chưa được thiết lập."
                );
            }

            try
            {
                // 4. Hash Password và tạo Entity
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
                string newId = _idGeneratorService.GenerateCustom(10);

                var account = new Account
                {
                    UserId = newId,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = AccountStatus.Active,
                    Role = request.Role,
                    AvatarUrl = null
                };

                // 5. Lưu vào Database
                await _accountRepository.AddAsync(account);
                await _accountRepository.SaveChangesAsync(cancellationToken);

                // 6. Gửi Email thông báo tài khoản
                try
                {
                    await _emailService.SendAccountInfoAsync(
                        request.Email,
                        request.FullName,
                        request.Email,
                        defaultPassword
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Tạo tài khoản {request.Role} thành công ({account.UserId}) nhưng gửi email thất bại.");
                }

                // 7. Trả về kết quả thành công
                string roleText = request.Role switch
                {
                    AccountRole.Admin => "Admin",
                    AccountRole.Staff => "Staff",
                    AccountRole.User => "User",
                    AccountRole.Moderator => "Moderator",
                    _ => request.Role.ToString()
                };

                return OperationResult<string>.Success(
                    account.UserId,
                    201,
                    $"Tạo tài khoản {roleText} và gửi email thông tin đăng nhập thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi xảy ra trong quá trình tạo tài khoản {request.Role}.");
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message
                );
            }
        }
    }
}