using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories; 
using Tokki.Application.UseCases.Leaderboard.DTOs;

namespace Tokki.Application.UseCases.Leaderboard.Queries
{
    public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, OperationResult<List<LeaderboardItemDto>>>
    {
        private readonly IAccountRepository _accountRepository; 

        public GetLeaderboardQueryHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<List<LeaderboardItemDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
        {
            var data = await _accountRepository.GetLeaderboardAsync(request.TimeFrame, request.Top);

            return OperationResult<List<LeaderboardItemDto>>.Success(data, 200);
        }
    }
}