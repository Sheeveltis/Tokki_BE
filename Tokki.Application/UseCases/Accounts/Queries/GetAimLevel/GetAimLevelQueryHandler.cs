using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetAimLevel
{
    public class GetAimLevelQueryHandler : IRequestHandler<GetAimLevelQuery, OperationResult<int?>>
    {
        private readonly IAccountRepository _accountRepository;

        public GetAimLevelQueryHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<int?>> Handle(GetAimLevelQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<int?>.Failure("Người dùng không tồn tại.", 404);
            }

            return OperationResult<int?>.Success(user.AimLevel, 200, "Lấy Aim Level thành công.");
        }
    }
}
