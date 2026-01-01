using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultsForAllUsers
{
    public class GetGameResultsForAllUsersQueryHandler
        : IRequestHandler<GetGameResultsForAllUsersQuery, OperationResult<PagedResult<GameResultDto>>>
    {
        private readonly IGameMatchSessionRepository _sessionRepository;

        public GetGameResultsForAllUsersQueryHandler(IGameMatchSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<OperationResult<PagedResult<GameResultDto>>> Handle(
            GetGameResultsForAllUsersQuery request,
            CancellationToken cancellationToken)
        {
            // Gọi repo: gameId, topicId, difficulty, pageNumber, pageSize
            var result = await _sessionRepository.GetPagedByGameTopicAsync(
                request.GameId,
                request.TopicId,
                request.gameDifficulty,
                request.PageNumber,
                request.PageSize
            );

            var sessions = result.Items;
            var totalCount = result.TotalCount;

            var dtos = sessions.Select(s => new GameResultDto
            {
                GameMatchSessionId = s.GameMatchSessionId,
                UserId = s.UserId,
                GameId = s.GameId,
                TopicId = s.TopicId,
                BestScore = s.BestScore,
                LatestScore = s.LatestScore,
                GameDifficulty = s.GameDifficulty,
                CreatedAt = s.CreatedAt
            }).ToList();

            var pagedResult = PagedResult<GameResultDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<GameResultDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách kết quả trò chơi thành công"
            );
        }
    }
}
