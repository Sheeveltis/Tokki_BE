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
    public class GetWordlePlayersQueryHandler : IRequestHandler<GetWordlePlayersQuery, OperationResult<List<WordlePlayerProgressDto>>>
    {
        private readonly IMiniGameRepository _repository;

        public GetWordlePlayersQueryHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<WordlePlayerProgressDto>>> Handle(GetWordlePlayersQuery request, CancellationToken token)
        {
            var players = await _repository.GetWordlePlayersAsync(request.DailyWordleId, token);

            var result = players.Select(p => new WordlePlayerProgressDto
            {
                UserId = p.UserId,
                UserName = p.User?.FullName ?? "Người dùng ẩn danh",
                AvatarUrl = p.User?.AvatarUrl,
                AttemptCount = p.AttemptCount,
                IsWon = p.IsWon,
                Guesses = p.Guesses,
                LastActivity = p.LastActivity
            }).ToList();

            return OperationResult<List<WordlePlayerProgressDto>>.Success(result);
        }
    }
}
