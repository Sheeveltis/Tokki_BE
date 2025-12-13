using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteTopic
{
    public class AddFavoriteTopicCommandHandler : IRequestHandler<AddFavoriteTopicCommand, OperationResult<string>>
    {
        private readonly IUserFavoriteTopicRepository _favoriteTopicRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AddFavoriteTopicCommandHandler> _logger;

        public AddFavoriteTopicCommandHandler(
            IUserFavoriteTopicRepository favoriteTopicRepository,
            ITopicRepository topicRepository,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AddFavoriteTopicCommandHandler> logger)
        {
            _favoriteTopicRepository = favoriteTopicRepository;
            _topicRepository = topicRepository;
            _idGeneratorService = idGeneratorService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(
            AddFavoriteTopicCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            try
            {
                // Kiểm tra Topic tồn tại
                var topic = await _topicRepository.GetByIdAsync(request.TopicId);
                if (topic == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // Kiểm tra đã tồn tại chưa
                var existing = await _favoriteTopicRepository.GetByUserAndTopicAsync(currentUserId, request.TopicId);
                if (existing != null)
                {
                    if (existing.Status == UserFavoriteTopicStatus.Active)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.FavoriteTopicAlreadyExists },
                            400,
                            AppErrors.FavoriteTopicAlreadyExists.Description
                        );
                    }

                    // Kích hoạt lại
                    existing.Status = UserFavoriteTopicStatus.Active;
                    existing.Note = request.Note;
                    existing.CreateDate = DateTime.UtcNow.AddHours(7);

                    await _favoriteTopicRepository.UpdateAsync(existing);
                    await _favoriteTopicRepository.SaveChangesAsync(cancellationToken);

                    return OperationResult<string>.Success(
                        existing.FavoriteTopicId,
                        200,
                        "Đã thêm lại chủ đề vào danh sách yêu thích."
                    );
                }

                // Tạo mới với ID generator
                string newId = _idGeneratorService.GenerateCustom(15);

                var favoriteTopic = new UserFavoriteTopic
                {
                    FavoriteTopicId = newId,
                    UserId = currentUserId,
                    TopicId = request.TopicId,
                    Note = request.Note,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = UserFavoriteTopicStatus.Active
                };

                await _favoriteTopicRepository.AddAsync(favoriteTopic);
                await _favoriteTopicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    favoriteTopic.FavoriteTopicId,
                    201,
                    "Thêm chủ đề vào danh sách yêu thích thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm chủ đề yêu thích: {TopicId}", request.TopicId);
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}