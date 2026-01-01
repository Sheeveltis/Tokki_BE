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
        private readonly IGameRepository _gameRepository;
        private readonly IGameMatchSessionRepository _sessionRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SaveGameResultCommandHandler> _logger;

        public SaveGameResultCommandHandler(
            IGameRepository gameRepository,
            IGameMatchSessionRepository sessionRepository,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SaveGameResultCommandHandler> logger)
        {
            _gameRepository = gameRepository;
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
                // 2. Kiểm tra game tồn tại & đang hoạt động
                var game = await _gameRepository.GetByIdAsync(request.GameId);
                if (game == null || game.Status != GameStatus.Active)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.GameNotFound },
                        404,
                        AppErrors.GameNotFound.Description
                    );
                }

                // 3. Lấy session hiện tại của user cho Game + Topic
                var session = await _sessionRepository.GetByUserGameTopicAsync(
                    request.UserId,
                    request.GameId,
                    request.TopicId
                );

                if (session == null)
                {
                    // Tạo mới
                    string newId = _idGeneratorService.GenerateCustom(15);

                    session = new GameMatchSession
                    {
                        GameMatchSessionId = newId,
                        UserId = request.UserId,
                        GameId = request.GameId,
                        TopicId = request.TopicId,
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
                    "Lỗi khi lưu kết quả game. UserId={UserId}, GameId={GameId}, TopicId={TopicId}",
                    request.UserId, request.GameId, request.TopicId);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
