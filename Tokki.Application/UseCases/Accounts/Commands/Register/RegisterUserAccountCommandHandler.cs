using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using BCrypt.Net;
using Tokki.Domain.Enums;
using FluentValidation; 

namespace Tokki.Application.UseCases.Accounts.Commands.Register
{
    public class RegisterUserAccountCommandHandler : IRequestHandler<RegisterUserAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<RegisterUserAccountCommandHandler> _logger;
        private readonly IValidator<RegisterUserAccountCommand> _validator; // ✅ THÊM

        public RegisterUserAccountCommandHandler(
            IAccountRepository accountRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<RegisterUserAccountCommandHandler> logger,
            IValidator<RegisterUserAccountCommand> validator) // ✅ THÊM
        {
            _accountRepository = accountRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
            _validator = validator; // ✅ THÊM
        }

        public async Task<OperationResult<string>> Handle(RegisterUserAccountCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                var error = new Error("Validation.Failed", errorMessages);
                return OperationResult<string>.Failure(error, 400, "Dữ liệu không hợp lệ");
            }

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
                    EmailVerified = false,
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