using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog; // Chứa RegisterUserAccountCommand
using BCrypt.Net;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.Register
{
    public class RegisterUserAccountCommandHandler : IRequestHandler<RegisterUserAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<RegisterUserAccountCommandHandler> _logger;

        public RegisterUserAccountCommandHandler(
            IAccountRepository accountRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<RegisterUserAccountCommandHandler> logger)
        {
            _accountRepository = accountRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(RegisterUserAccountCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate: Kiểm tra Email đã tồn tại chưa
            bool emailExists = await _accountRepository.IsEmailExistsAsync(request.Email);

            if (emailExists)
            {
                // Bạn nên thay new Error(...) bằng AppErrors.EmailDuplicated như comment của bạn
                var error = new Error("User.EmailDuplicated", "Email này đã được sử dụng.");

                // ✅ THÊM: 400 (Bad Request) và message hiển thị cho UI
                return OperationResult<string>.Failure(error, 400, "Email đã tồn tại");
            }

            try
            {
                // 2. Tạo ID và Hash password
                string newId = _idGeneratorService.GenerateCustom(10);
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 3. Mapping sang Entity ACCOUNT
                var accountEntity = new Account
                {
                    UserId = newId,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FullName = request.FullName,
                    AvatarUrl="null",
                    EmailVerified=false,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth,
                    CreatedAt = DateTime.UtcNow,
                    Status = AccountStatus.Active, 
                    Role = AccountRole.User
                };

                // 4. Lưu vào DB
                await _accountRepository.AddAsync(accountEntity);
                await _accountRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    accountEntity.UserId,
                    201,
                    OperationMessages.CreateSuccess("Tài khoản")
                );
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Đăng ký thất bại: {RealError}", realError);

                return OperationResult<string>.Failure(
                    AppErrors.ServerError,
                    500,
                    $"Lỗi hệ thống: {realError}"
                );
            }
        }
    }
}