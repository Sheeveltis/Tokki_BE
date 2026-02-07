using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel
{
    public class UpdateMyLevelCommandHandler : IRequestHandler<UpdateMyLevelCommand, OperationResult<bool>>
    {
        private readonly IAccountRepository _accountRepository;

        public UpdateMyLevelCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateMyLevelCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                return OperationResult<bool>.Failure(new List<Error> { AppErrors.UserNotFound }, 404, "Không tìm thấy người dùng.");
            }

            var nowLocal = DateTime.UtcNow.AddHours(7);

            // Nếu bạn muốn chặn giống login
            if (user.Status == AccountStatus.Inactive)
                return OperationResult<bool>.Failure(new List<Error> { AppErrors.AccountInActive }, 403, "Tài khoản của bạn không hoạt động.");

            if (user.Status == AccountStatus.Banned)
                return OperationResult<bool>.Failure(new List<Error> { AppErrors.AccountBanned }, 403, "Tài khoản của bạn đã bị khóa vĩnh viễn.");

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > nowLocal)
            {
                var remainingMinutes = (int)Math.Ceiling((user.LockedUntil.Value - nowLocal).TotalMinutes);
                return OperationResult<bool>.Failure(new List<Error> { AppErrors.AccountLocked }, 403, $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
            }

            // Validate enum nếu Level != null
            if (request.Level.HasValue)
            {
                if (!Enum.IsDefined(typeof(TopicLevel), request.Level.Value))
                {
                    return OperationResult<bool>.Failure("Level không hợp lệ.", 400);
                }

                user.Level = (TopicLevel)request.Level.Value;
            }
            else
            {
                user.Level = null;
            }

            user.UpdatedAt = nowLocal;

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            // Nếu bạn không muốn message: có thể để message = "" (nhưng Success() đang default "Thành công")
            return OperationResult<bool>.Success(true, 200, "Cập nhật thành công");
        }
    }
}
