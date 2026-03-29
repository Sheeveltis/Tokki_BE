using MediatR;
using System.Collections.Generic;
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
        private readonly IAccountRepository _accountRepository;

        public GetGameResultsForAllUsersQueryHandler(
            IGameMatchSessionRepository sessionRepository,
            IAccountRepository accountRepository)
        {
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
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

            // Lấy thông tin user tuần tự, cache theo userId để tránh query trùng
            var userInfoCache = new Dictionary<string, Tokki.Application.UseCases.Accounts.DTOs.AccountBasicInfoDTO?>();
            foreach (var s in sessions)
            {
                if (!userInfoCache.ContainsKey(s.UserId))
                {
                    userInfoCache[s.UserId] = await _accountRepository.GetBasicInfoAsync(s.UserId);
                }
            }

            var dtos = sessions.Select(s =>
            {
                var info = userInfoCache.GetValueOrDefault(s.UserId);
                return new GameResultDto
                {
                    GameMatchSessionId = s.GameMatchSessionId,
                    UserId = s.UserId,
                    UserName = info?.FullName ?? string.Empty,
                    AvatarUrl = info?.AvatarUrl,
                    TitleName = info?.CurrentTitleName,
                    TitleColorHex = info?.CurrentColorHexTitle,
                    TitleIconUrl = info?.TitleIconUrl,
                    GameId = s.GameId,
                    TopicId = s.TopicId,
                    BestScore = s.BestScore,
                    LatestScore = s.LatestScore,
                    GameDifficulty = s.GameDifficulty,
                    CreatedAt = s.CreatedAt
                };
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
