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
    public class CreateStaffAndAdminAccountCommandHandler : IRequestHandler<CreateStaffAndAdminAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateStaffAndAdminAccountCommandHandler> _logger;

        public CreateStaffAndAdminAccountCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IIdGeneratorService idGeneratorService,
            IEmailService emailService,
            ILogger<CreateStaffAndAdminAccountCommandHandler> logger)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _idGeneratorService = idGeneratorService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateStaffAndAdminAccountCommand request, CancellationToken cancellationToken)
        {
            // 0. Validate Role (chỉ cho phép tạo Staff hoặc Admin)
            if (request.Role != AccountRole.Staff && request.Role != AccountRole.Admin)
            {
                return OperationResult<string>.Failure(
                    new List<Error> {
                        new Error("INVALID_ROLE", "Chỉ được phép tạo tài khoản Staff hoặc Admin.")
                    },
                    400,
                    "Vai trò không hợp lệ."
                );
            }

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

            // 3. Lấy Password mặc định từ SystemConfig (có thể khác nhau cho Admin và Staff)
            string configKey = request.Role == AccountRole.Admin
                ? "DEFAULT_PASSWORD_FOR_ADMIN"
                : "DEFALUT_PASSWORD_FOR_STAFF";

            string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync(configKey);

            // Fallback: nếu không có config riêng cho Admin, dùng config của Staff
            if (string.IsNullOrEmpty(defaultPassword) && request.Role == AccountRole.Admin)
            {
                defaultPassword = await _systemConfigRepository.GetValueByKeyAsync("DEFALUT_PASSWORD_FOR_STAFF");
            }

            if (string.IsNullOrEmpty(defaultPassword))
            {
                _logger.LogError($"Cấu hình {configKey} không được tìm thấy hoặc rỗng.");
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
                    Role = request.Role, // Sử dụng Role từ request
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
                string roleText = request.Role == AccountRole.Admin ? "Admin" : "Staff";
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