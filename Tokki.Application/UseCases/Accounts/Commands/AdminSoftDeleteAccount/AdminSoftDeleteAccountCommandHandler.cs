using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                return OperationResult<string>.Failure(new List<Error>
                {
                    new Error("Account.TargetUserIdRequired", "Thiếu userId cần xóa.")
                });
            }

            var user = await _accountRepository.GetByIdAsync(request.TargetUserId);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }

            // Nếu đã bị xóa mềm trước đó
            if (user.Status == AccountStatus.Inactive)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    new Error("Account.AlreadyDeleted", "Tài khoản đã bị xóa trước đó.")
                });
            }

            user.Status = AccountStatus.Inactive; // hoặc AccountStatus.Invalid nếu enum của bạn có
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // (Tuỳ chọn) audit
            // user.DeletedBy = request.AdminUserId;
            // user.DeletedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Admin đã xóa mềm tài khoản thành công!", 200);
        }
    }
}
