using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Solitaire.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultsForAllUsers
{
    public class GetSolitaireResultsForAllUsersQueryHandler
        : IRequestHandler<GetSolitaireResultsForAllUsersQuery, OperationResult<PagedResult<SolitaireResultDto>>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ISolitaireSessionRepository _sessionRepository;
        private readonly IAccountRepository _accountRepository;

        public GetSolitaireResultsForAllUsersQueryHandler(
            IGameRepository gameRepository,
            ISolitaireSessionRepository sessionRepository,
            IAccountRepository accountRepository)
        {
            _gameRepository = gameRepository;
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<PagedResult<SolitaireResultDto>>> Handle(
            GetSolitaireResultsForAllUsersQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra game tồn tại & đúng loại Solitaire
            var game = await _gameRepository.GetByIdAsync(request.GameId);
            if (game == null || game.Status != GameStatus.Active)
            {
                return OperationResult<PagedResult<SolitaireResultDto>>.Failure(
                    new List<Error> { AppErrors.GameNotFound },
                    404,
                    AppErrors.GameNotFound.Description
                );
            }

            if (game.GameType != GameType.Solitaire)
            {
                return OperationResult<PagedResult<SolitaireResultDto>>.Failure(
                    new List<Error> { AppErrors.GameTypeMismatch },
                    400,
                    AppErrors.GameTypeMismatch.Description
                );
            }

            // 2. Lấy dữ liệu phân trang
            var result = await _sessionRepository.GetPagedByGameAsync(
                request.GameId,
                request.GameDifficulty,
                request.PageNumber,
                request.PageSize
            );

            // Lấy tên user tuần tự để tránh lỗi DbContext concurrency (EF Core không thread-safe)
            var userNameCache = new Dictionary<string, string>();
            foreach (var s in result.Items)
            {
                if (!userNameCache.ContainsKey(s.UserId))
                {
                    var account = await _accountRepository.GetByIdAsync(s.UserId);
                    userNameCache[s.UserId] = account?.FullName ?? string.Empty;
                }
            }

            var dtos = result.Items.Select(s => new SolitaireResultDto
            {
                GameMatchSessionId = s.GameMatchSessionId,
                UserId = s.UserId,
                UserName = userNameCache.GetValueOrDefault(s.UserId, string.Empty),
                GameId = s.GameId,
                BestScore = s.BestScore,
                LatestScore = s.LatestScore,
                GameDifficulty = s.GameDifficulty,
                CreatedAt = s.CreatedAt
            }).ToList();

            var pagedResult = PagedResult<SolitaireResultDto>.Create(
                dtos,
                result.TotalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<SolitaireResultDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách kết quả Solitaire thành công"
            );
        }
    }
}
