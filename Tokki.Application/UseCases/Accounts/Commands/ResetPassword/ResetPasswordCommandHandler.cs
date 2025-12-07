using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Accounts.Commands.ResetPassword
{
    // UseCases/Accounts/Commands/ForgotPassword/ResetPassword/ResetPasswordCommandHandler.cs
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<ResetPasswordCommand> _validator;

        public ResetPasswordCommandHandler(IAccountRepository accountRepository, IValidator<ResetPasswordCommand> validator)
        {
            _accountRepository = accountRepository;
            _validator = validator;
        }

        // ResetPasswordCommandHandler.cs
        public async Task<OperationResult<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null) return OperationResult<string>.Failure("User không tồn tại.", 404);

            // 2. Đổi mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Reset các biến khóa
            user.FailedLoginCount = 0;
            user.LockedUntil = null;

            // 3. Lưu DB
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Đổi mật khẩu thành công!", 200);
        }

    }
}
