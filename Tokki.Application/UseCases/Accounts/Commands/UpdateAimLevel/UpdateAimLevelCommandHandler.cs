using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateAimLevel
{
    public class UpdateAimLevelCommandHandler : IRequestHandler<UpdateAimLevelCommand, OperationResult<bool>>
    {
        private readonly IAccountRepository _accountRepository;

        public UpdateAimLevelCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateAimLevelCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return OperationResult<bool>.Failure("UserId is required.", 400);
            }

            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<bool>.Failure("User not found.", 404);
            }

            // Cập nhật mục tiêu Topik
            user.AimLevel = request.AimLevel;
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Cập nhật mục tiêu trình độ Topik thành công.");
        }
    }
}
