using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;

        public DeleteAccountCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserUnauthorized });
            }

            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }

            // Kiểm tra xem tài khoản đã bị xóa mềm chưa
            if (user.Status == AccountStatus.Inactive)
            {
                return OperationResult<string>.Failure(new List<Error>
                {
                    new Error("Account.AlreadyDeleted", "Tài khoản đã bị xóa trước đó.")
                });
            }

            user.Status = AccountStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // Có thể thêm thêm logic:
            // - Xóa session hiện tại
            // - Hủy VIP subscription
            // - Anonymize một số thông tin nhạy cảm (tuỳ chọn)
            // user.Email = $"deleted_{user.UserId}@deleted.com";
            // user.PhoneNumber = null;
            // user.AvatarUrl = null;

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Tài khoản đã được xóa thành công!", 200);
        }
    }
}
