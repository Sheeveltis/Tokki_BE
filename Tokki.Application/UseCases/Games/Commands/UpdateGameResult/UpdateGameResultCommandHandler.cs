using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Commands.UpdateGameResult
{
    public class UpdateGameResultCommandHandler
        : IRequestHandler<UpdateGameResultCommand, OperationResult<bool>>
    {

        private readonly IGameMatchSessionRepository _sessionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateGameResultCommandHandler> _logger;

        public UpdateGameResultCommandHandler(

            IGameMatchSessionRepository sessionRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateGameResultCommandHandler> logger)
        {

            _sessionRepository = sessionRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            UpdateGameResultCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy UserId từ token
            var currentUserId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            request.UserId = currentUserId;

            try
            {
                // 3. Lấy session hiện tại của user cho Game + Topic + Difficulty
                var session = await _sessionRepository.GetByUserGameTopicAsync(
                    request.UserId,
                    request.GameType,
                    request.TopicId,
                    request.GameDifficulty
                );

                // VALID: phải có bản ghi rồi mới cho update
                if (session == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.GameResultNotFound },
                        404,
                        AppErrors.GameResultNotFound.Description
                    );
                }

                // 4. Cập nhật: điểm hiện tại + điểm cao nhất
                session.LatestScore = request.Score;

                if (request.Score > session.BestScore)
                {
                    session.BestScore = request.Score;
                }

                // Không đổi GameDifficulty / CreatedAt

                await _sessionRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Cập nhật kết quả trò chơi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi cập nhật kết quả game. UserId={UserId}, GameType={GameType}, TopicId={TopicId}, Difficulty={Difficulty}",
                    request.UserId, request.GameType, request.TopicId, request.GameDifficulty);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
