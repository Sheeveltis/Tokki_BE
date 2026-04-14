using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.SetVipRole
{
    public class SetVipRoleCommandHandler : IRequestHandler<SetVipRoleCommand, OperationResult<bool>>
    {
        private readonly IAccountRepository _accountRepository;

        public SetVipRoleCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<bool>> Handle(SetVipRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy người dùng", 404);
            }

            user.Role = AccountRole.Vip;
            
            // Set VipExpirationDate = Ngày hiện tại + số ngày truyền vào
            // Sử dụng UTC+7 như các phần khác của hệ thống cho sự nhất quán
            var now = DateTimeOffset.UtcNow.AddHours(7);
            user.VipExpirationDate = now.AddDays(request.Days);
            
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
