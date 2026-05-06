using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordlePlayersQueryHandler : IRequestHandler<GetWordlePlayersQuery, OperationResult<PagedResult<WordlePlayerProgressDto>>>
    {
        private readonly IMiniGameRepository _repository;

        public GetWordlePlayersQueryHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<WordlePlayerProgressDto>>> Handle(GetWordlePlayersQuery request, CancellationToken token)
        {
            var (players, totalCount) = await _repository.GetWordlePlayersAsync(request.DailyWordleId, request.PageIndex, request.PageSize, token);

            var items = players.Select(p => new WordlePlayerProgressDto
            {
                UserId = p.UserId,
                UserName = p.User?.FullName ?? "Người dùng ẩn danh",
                AvatarUrl = p.User?.AvatarUrl,
                AttemptCount = p.AttemptCount,
                IsWon = p.IsWon,
                Guesses = p.Guesses,
                LastActivity = p.LastActivity
            }).ToList();

            var pagedResult = PagedResult<WordlePlayerProgressDto>.Create(items, totalCount, request.PageIndex, request.PageSize);

            return OperationResult<PagedResult<WordlePlayerProgressDto>>.Success(pagedResult);
        }
    }
}
