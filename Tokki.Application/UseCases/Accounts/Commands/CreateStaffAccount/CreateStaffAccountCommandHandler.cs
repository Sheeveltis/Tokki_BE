using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaff
{
    public class CreateStaffAccountCommandHandler : IRequestHandler<CreateStaffAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateStaffAccountCommandHandler> _logger; // Thêm logger để ghi lại lỗi gửi email

        public CreateStaffAccountCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IIdGeneratorService idGeneratorService,
            IEmailService emailService,
            ILogger<CreateStaffAccountCommandHandler> logger)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _idGeneratorService = idGeneratorService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateStaffAccountCommand request, CancellationToken cancellationToken)
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

            // 3. Lấy Password mặc định từ SystemConfig
            const string configKey = "DEFALUT_PASSWORD_FOR_STAFF";
            string? defaultPassword = await _systemConfigRepository.GetValueByKeyAsync(configKey);

            if (string.IsNullOrEmpty(defaultPassword))
            {
                _logger.LogError($"Cấu hình {configKey} không được tìm thấy hoặc rỗng.");
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    "Cấu hình mật khẩu mặc định cho nhân viên chưa được thiết lập."
                );
            }

            try
            {
                // 4. Hash Password và tạo Entity
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
                string newId = _idGeneratorService.GenerateCustom(10);

                var staffAccount = new Account
                {
                    UserId = newId,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    // Chuyển DateOnly sang DateTime (chỉ lấy ngày)
                    DateOfBirth = request.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = AccountStatus.Active,
                    Role = AccountRole.Staff, // Đặt vai trò là Staff
                    AvatarUrl = null
                };

                // 5. Lưu vào Database
                await _accountRepository.AddAsync(staffAccount);
                await _accountRepository.SaveChangesAsync(cancellationToken);

                // 6. Gửi Email thông báo tài khoản
                try
                {
                    await _emailService.SendAccountInfoAsync(
                        request.Email,
                        request.FullName,
                        request.Email,
                        defaultPassword // Gửi mật khẩu thô để nhân viên đăng nhập
                    );
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi gửi email, nhưng vẫn trả về thành công việc tạo tài khoản
                    _logger.LogError(ex, $"Tạo tài khoản Staff thành công ({staffAccount.UserId}) nhưng gửi email thất bại.");
                }

                // 7. Trả về kết quả thành công
                return OperationResult<string>.Success(
                    staffAccount.UserId,
                    201,
                    "Tạo tài khoản và gửi email thông tin đăng nhập thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo tài khoản Staff.");
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message
                );
            }
        }
    }
}