using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Domain.Entities;
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
            // ✅ XÓA phần validate thủ công - ValidationBehavior đã xử lý rồi

            // Kiểm tra email tồn tại (Logic nghiệp vụ, không phải validation cơ bản)
            bool emailExists = await _accountRepository.IsEmailExistsAsync(request.Email);
            if (emailExists)
            {
                var error = new Error("User.EmailDuplicated", "Email này đã được sử dụng.");
                return OperationResult<string>.Failure(error, 400, "Email đã tồn tại");
            }

            try
            {
                string newId = _idGeneratorService.GenerateCustom(10);
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var accountEntity = new Account
                {
                    UserId = newId,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FullName = request.FullName,
                    AvatarUrl = "null",
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = AccountStatus.Active,
                    Role = AccountRole.User
                };

                await _accountRepository.AddAsync(accountEntity);
                await _accountRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    accountEntity.UserId,
                    201,
                    "Đăng ký tài khoản thành công"
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