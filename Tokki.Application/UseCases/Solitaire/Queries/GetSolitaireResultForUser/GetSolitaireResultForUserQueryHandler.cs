using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Solitaire.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultForUser
{
    public class GetSolitaireResultForUserQueryHandler
        : IRequestHandler<GetSolitaireResultForUserQuery, OperationResult<SolitaireResultDto?>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ISolitaireSessionRepository _sessionRepository;
        private readonly IAccountRepository _accountRepository;

        public GetSolitaireResultForUserQueryHandler(
            IGameRepository gameRepository,
            ISolitaireSessionRepository sessionRepository,
            IAccountRepository accountRepository)
        {
            _gameRepository = gameRepository;
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<SolitaireResultDto?>> Handle(
            GetSolitaireResultForUserQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra game tồn tại & đúng loại Solitaire
            var game = await _gameRepository.GetByIdAsync(request.GameId);
            if (game == null || game.Status != GameStatus.Active)
            {
                return OperationResult<SolitaireResultDto?>.Failure(
                    new List<Error> { AppErrors.GameNotFound },
                    404,
                    AppErrors.GameNotFound.Description
                );
            }

            if (game.GameType != GameType.Solitaire)
            {
                return OperationResult<SolitaireResultDto?>.Failure(
                    new List<Error> { AppErrors.GameTypeMismatch },
                    400,
                    AppErrors.GameTypeMismatch.Description
                );
            }

            // 2. Lấy session
            var session = await _sessionRepository.GetByUserGameAsync(
                request.UserId,
                request.GameId,
                request.GameDifficulty
            );

            if (session == null)
            {
                return OperationResult<SolitaireResultDto?>.Failure(
                    new List<Error> { AppErrors.GameResultNotFound },
                    404,
                    AppErrors.GameResultNotFound.Description
                );
            }

            var userInfo = await _accountRepository.GetBasicInfoAsync(session.UserId);

            var dto = new SolitaireResultDto
            {
                GameMatchSessionId = session.GameMatchSessionId,
                UserId = session.UserId,
                UserName = userInfo?.FullName ?? string.Empty,
                AvatarUrl = userInfo?.AvatarUrl,
                TitleName = userInfo?.CurrentTitleName,
                TitleColorHex = userInfo?.CurrentColorHexTitle,
                TitleIconUrl = userInfo?.TitleIconUrl,
                GameId = session.GameId,
                BestScore = session.BestScore,
                LatestScore = session.LatestScore,
                GameDifficulty = session.GameDifficulty,
                CreatedAt = session.CreatedAt
            };

            return OperationResult<SolitaireResultDto?>.Success(
                dto,
                200,
                "Lấy kết quả Solitaire thành công"
            );
        }
    }
}
