using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultForUser
{
    public class GetGameResultForUserQueryHandler
        : IRequestHandler<GetGameResultForUserQuery, OperationResult<PagedResult<GameResultDto>>>
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

        public async Task<OperationResult<PagedResult<GameResultDto>>> Handle(
            GetGameResultForUserQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionRepository.GetAllByUserAsync(
                request.UserId,
                request.GameType,
                request.TopicId,
                request.GameDifficulty,
                request.PageNumber,
                request.PageSize
            );

            var sessions = result.Items;
            var totalCount = result.TotalCount;

            if (sessions == null || !sessions.Any())
            {
                var emptyPaged = PagedResult<GameResultDto>.Create(
                    new List<GameResultDto>(), 0, request.PageNumber, request.PageSize);
                return OperationResult<PagedResult<GameResultDto>>.Success(
                    emptyPaged,
                    200,
                    "Không tìm thấy kết quả trò chơi"
                );
            }

            var userInfo = await _accountRepository.GetBasicInfoAsync(request.UserId);
            var dtoList = new List<GameResultDto>();

            foreach (var session in sessions)
            {
                var dto = new GameResultDto
                {
                    GameMatchSessionId = session.GameMatchSessionId,
                    UserId = session.UserId,
                    UserName = userInfo?.FullName ?? string.Empty,
                    AvatarUrl = userInfo?.AvatarUrl,
                    TitleName = userInfo?.CurrentTitleName,
                    TitleColorHex = userInfo?.CurrentColorHexTitle,
                    TitleIconUrl = userInfo?.TitleIconUrl,
                    GameType = session.GameType,
                    TopicId = session.TopicId,
                    BestScore = session.BestScore,
                    LatestScore = session.LatestScore,
                    GameDifficulty = session.GameDifficulty,
                    CreatedAt = session.CreatedAt
                };
                dtoList.Add(dto);
            }

            var pagedResult = PagedResult<GameResultDto>.Create(
                dtoList, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<GameResultDto>>.Success(
                pagedResult,
                200,
                "Lấy kết quả trò chơi thành công"
            );
        }
    }
}
