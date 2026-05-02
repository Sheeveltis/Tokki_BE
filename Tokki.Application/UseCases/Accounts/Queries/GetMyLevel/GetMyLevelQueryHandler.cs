using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetMyLevel
{
    public class GetMyLevelQueryHandler : IRequestHandler<GetMyLevelQuery, OperationResult<GetMyLevelResponse>>
    {
        private readonly IAccountRepository _accountRepository;

        public GetMyLevelQueryHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<GetMyLevelResponse>> Handle(GetMyLevelQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                return OperationResult<GetMyLevelResponse>.Failure(
                    new List<Error> { AppErrors.UserNotFound },
                    404,
                    "Không tìm thấy người dùng."
                );
            }

  
         
            return OperationResult<GetMyLevelResponse>.Success(new GetMyLevelResponse
            {
                Level = user.Level
            }, 200, "");
        }
    }

}
