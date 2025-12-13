using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteTopic
{
    public class RemoveFavoriteTopicCommandHandler : IRequestHandler<RemoveFavoriteTopicCommand, OperationResult<bool>>
    {
        private readonly IUserFavoriteTopicRepository _favoriteTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RemoveFavoriteTopicCommandHandler(
            IUserFavoriteTopicRepository favoriteTopicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _favoriteTopicRepository = favoriteTopicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<bool>> Handle(
            RemoveFavoriteTopicCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var favoriteTopic = await _favoriteTopicRepository.GetByUserAndTopicAsync(currentUserId, request.TopicId);
            if (favoriteTopic == null || favoriteTopic.Status == UserFavoriteTopicStatus.Removed)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.FavoriteTopicNotFound },
                    404
                );
            }

            if (request.ForceDelete)
            {
                await _favoriteTopicRepository.DeleteAsync(favoriteTopic);
            }
            else
            {
                favoriteTopic.Status = UserFavoriteTopicStatus.Removed;
                await _favoriteTopicRepository.UpdateAsync(favoriteTopic);
            }

            await _favoriteTopicRepository.SaveChangesAsync(cancellationToken);

            string message = request.ForceDelete
                ? "Xóa vĩnh viễn chủ đề yêu thích thành công."
                : "Đã bỏ chủ đề khỏi danh sách yêu thích.";

            return OperationResult<bool>.Success(true, 200, message);
        }
    }
}
