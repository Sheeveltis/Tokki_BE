using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Application.UseCases.Accounts.Commands.Register; // Sửa namespace cho đúng
using BCrypt.Net;
using Tokki.Domain.Enums;
using FluentValidation;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
namespace Tokki.Application.UseCases.Accounts.Commands.Register
{
    public class RegisterUserAccountCommandHandler : IRequestHandler<RegisterUserAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<RegisterUserAccountCommandHandler> _logger;
        private readonly IValidator<RegisterUserAccountCommand> _validator;
        public RegisterUserAccountCommandHandler(
            IAccountRepository accountRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<RegisterUserAccountCommandHandler> logger,
            IValidator<RegisterUserAccountCommand> validator)
        {
            _accountRepository = accountRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
            _validator = validator;
        }
        public async Task<OperationResult<string>> Handle(RegisterUserAccountCommand request, CancellationToken cancellationToken)
        {

            bool emailExists = await _accountRepository.IsEmailExistsAsync(request.Email);
            if (emailExists)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.EmailDuplicated },
                    409,
                    AppErrors.EmailDuplicated.Description
                );
            }
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                bool phoneExists = await _accountRepository.IsPhoneNumberExistsAsync(request.PhoneNumber);
                if (phoneExists)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PhoneNumberDuplicated },
                        409, // Conflict
                        AppErrors.PhoneNumberDuplicated.Description
                    );
                }
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
                    AvatarUrl = null,
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

                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}