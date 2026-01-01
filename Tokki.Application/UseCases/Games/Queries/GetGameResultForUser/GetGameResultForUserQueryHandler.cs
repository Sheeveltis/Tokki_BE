using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultForUser
{
    public class GetGameResultForUserQueryHandler
        : IRequestHandler<GetGameResultForUserQuery, OperationResult<GameResultDto?>>
    {
        private readonly IGameMatchSessionRepository _sessionRepository;

        public GetGameResultForUserQueryHandler(IGameMatchSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<OperationResult<GameResultDto?>> Handle(
        GetGameResultForUserQuery request,
        CancellationToken cancellationToken)
        {
            var session = await _sessionRepository.GetByUserGameTopicAsync(
                request.UserId,
                request.GameId,
                request.TopicId,
                request.GameDifficulty   // lọc thêm theo độ khó
            );

            if (session == null)
            {
                return OperationResult<GameResultDto?>.Failure(
                    new List<Error> { AppErrors.GameResultNotFound },
                    404,
                    AppErrors.GameResultNotFound.Description
                );
            }

            var dto = new GameResultDto
            {
                GameMatchSessionId = session.GameMatchSessionId,
                UserId = session.UserId,
                GameId = session.GameId,
                TopicId = session.TopicId,
                BestScore = session.BestScore,
                LatestScore = session.LatestScore,
                GameDifficulty = session.GameDifficulty,
                CreatedAt = session.CreatedAt
            };

            return OperationResult<GameResultDto?>.Success(
                dto,
                200,
                "Lấy kết quả trò chơi thành công"
            );
        }

    }
}
