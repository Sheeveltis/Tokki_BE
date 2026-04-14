using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Games.Queries.CheckUserPlayedLevel
{
    public class CheckUserPlayedLevelQueryHandler
       : IRequestHandler<CheckUserPlayedLevelQuery, OperationResult<bool>>
    {
        private readonly IGameMatchSessionRepository _sessionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CheckUserPlayedLevelQueryHandler> _logger;

        public CheckUserPlayedLevelQueryHandler(
            IGameMatchSessionRepository sessionRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CheckUserPlayedLevelQueryHandler> logger)
        {
            _sessionRepository = sessionRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            CheckUserPlayedLevelQuery request,
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
                // 2. Kiểm tra trong GameMatchSessions
                var session = await _sessionRepository.GetByUserGameTopicAsync(
                    request.UserId,
                    request.GameType,
                    request.TopicId,
                    request.GameDifficulty
                );

                bool hasPlayed = session != null;

                return OperationResult<bool>.Success(
                    hasPlayed,
                    200,
                    "Kiểm tra lịch sử chơi level thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                "Lỗi khi kiểm tra level. UserId={UserId}, GameType={GameType}, TopicId={TopicId}, Difficulty={Difficulty}",
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
