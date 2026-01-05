using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount
{
    public class AdminSoftDeleteAccountCommandHandler
         : IRequestHandler<AdminSoftDeleteAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;

        public AdminSoftDeleteAccountCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(
            AdminSoftDeleteAccountCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AdminUserId))
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserUnauthorized });
            }

            if (string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.TargetUserIdRequired });
            }

            if (string.Equals(request.AdminUserId.Trim(), request.TargetUserId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.CannotDisableSelf });
            }

            var user = await _accountRepository.GetByIdAsync(request.TargetUserId);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }

            if (user.Status == AccountStatus.Inactive)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.AccountAlreadyInactive });
            }

            user.Status = AccountStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Vô hiệu hóa tài khoản của người dùng thành công.", 200);
        }
    }
}
