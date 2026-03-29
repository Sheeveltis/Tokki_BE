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
        private readonly IAccountRepository _accountRepository;

        public GetGameResultForUserQueryHandler(
            IGameMatchSessionRepository sessionRepository,
            IAccountRepository accountRepository)
        {
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<GameResultDto?>> Handle(
            GetGameResultForUserQuery request,
            CancellationToken cancellationToken)
        {
            var session = await _sessionRepository.GetByUserGameTopicAsync(
                request.UserId,
                request.GameId,
                request.TopicId,
                request.GameDifficulty
            );

            if (session == null)
            {
                return OperationResult<GameResultDto?>.Failure(
                    new List<Error> { AppErrors.GameResultNotFound },
                    404,
                    AppErrors.GameResultNotFound.Description
                );
            }

            var userInfo = await _accountRepository.GetBasicInfoAsync(session.UserId);

            var dto = new GameResultDto
            {
                GameMatchSessionId = session.GameMatchSessionId,
                UserId = session.UserId,
                UserName = userInfo?.FullName ?? string.Empty,
                AvatarUrl = userInfo?.AvatarUrl,
                TitleName = userInfo?.CurrentTitleName,
                TitleColorHex = userInfo?.CurrentColorHexTitle,
                TitleIconUrl = userInfo?.TitleIconUrl,
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
