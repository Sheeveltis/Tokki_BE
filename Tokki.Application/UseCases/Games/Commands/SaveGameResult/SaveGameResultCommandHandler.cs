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
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Commands.SaveGameResult
{
    public class SaveGameResultCommandHandler
        : IRequestHandler<SaveGameResultCommand, OperationResult<bool>>
    {

        private readonly IGameMatchSessionRepository _sessionRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SaveGameResultCommandHandler> _logger;

        public SaveGameResultCommandHandler(

            IGameMatchSessionRepository sessionRepository,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SaveGameResultCommandHandler> logger)
        {

            _sessionRepository = sessionRepository;
            _idGeneratorService = idGeneratorService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            SaveGameResultCommand request,
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

                if (session == null)
                {
                    // Tạo mới cho độ khó này
                    string newId = _idGeneratorService.GenerateCustom(15);

                    session = new GameMatchSession
                    {
                        GameMatchSessionId = newId,
                        UserId = request.UserId,
                        GameType = request.GameType,
                        TopicId = request.TopicId,
                        GameDifficulty = request.GameDifficulty,
                        BestScore = request.Score,
                        LatestScore = request.Score,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };

                    await _sessionRepository.AddAsync(session);
                }
                else
                {
                    // Cập nhật: điểm hiện tại + điểm cao nhất
                    session.LatestScore = request.Score;

                    if (request.Score > session.BestScore)
                    {
                        session.BestScore = request.Score;
                    }

                    // Không đổi GameDifficulty / CreatedAt
                }

                await _sessionRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Lưu kết quả trò chơi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi lưu kết quả game. UserId={UserId}, GameType={GameType}, TopicId={TopicId}, Difficulty={Difficulty}",
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
