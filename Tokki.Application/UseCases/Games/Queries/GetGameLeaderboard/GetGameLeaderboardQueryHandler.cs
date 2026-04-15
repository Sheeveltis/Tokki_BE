using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameLeaderboard
{
    public class GetGameLeaderboardQueryHandler
        : IRequestHandler<GetGameLeaderboardQuery, OperationResult<PagedResult<GameLeaderboardDto>>>
    {
        private readonly IGameMatchSessionRepository _sessionRepository;
        private readonly IAccountRepository _accountRepository;

        public GetGameLeaderboardQueryHandler(
            IGameMatchSessionRepository sessionRepository,
            IAccountRepository accountRepository)
        {
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<PagedResult<GameLeaderboardDto>>> Handle(
            GetGameLeaderboardQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionRepository.GetLeaderboardAsync(
                request.GameType,
                request.GameDifficulty,
                request.TopicId,
                request.PageNumber,
                request.PageSize
            );

            var items = result.Items;
            var dtos = new List<GameLeaderboardDto>();

            foreach (var item in items)
            {
                var userInfo = await _accountRepository.GetBasicInfoAsync(item.UserId);
                dtos.Add(new GameLeaderboardDto
                {
                    UserId = item.UserId,
                    UserName = userInfo?.FullName ?? string.Empty,
                    AvatarUrl = userInfo?.AvatarUrl,
                    TitleName = userInfo?.CurrentTitleName,
                    TitleColorHex = userInfo?.CurrentColorHexTitle,
                    TitleIconUrl = userInfo?.TitleIconUrl,
                    GameType = item.GameType,
                    GameDifficulty = item.GameDifficulty,
                    TopicId = item.TopicId,
                    BestScore = item.BestScore
                });
            }

            var pagedResult = PagedResult<GameLeaderboardDto>.Create(
                dtos,
                result.TotalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<GameLeaderboardDto>>.Success(
                pagedResult,
                200,
                "Lấy bảng xếp hạng thành công"
            );
        }
    }
}
