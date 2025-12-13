using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

using System.Security.Claims;


namespace Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteWord
{
    public class RemoveFavoriteWordCommandHandler : IRequestHandler<RemoveFavoriteWordCommand, OperationResult<bool>>
    {
        private readonly IUserFavoriteWordRepository _favoriteWordRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RemoveFavoriteWordCommandHandler(
            IUserFavoriteWordRepository favoriteWordRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _favoriteWordRepository = favoriteWordRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<bool>> Handle(
            RemoveFavoriteWordCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var favoriteWord = await _favoriteWordRepository.GetByUserAndWordAsync(currentUserId, request.WordId);
            if (favoriteWord == null || favoriteWord.Status == UserFavoriteWordStatus.Removed)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.FavoriteWordNotFound },
                    404
                );
            }

            if (request.ForceDelete)
            {
                await _favoriteWordRepository.DeleteAsync(favoriteWord);
            }
            else
            {
                favoriteWord.Status = UserFavoriteWordStatus.Removed;
                await _favoriteWordRepository.UpdateAsync(favoriteWord);
            }

            await _favoriteWordRepository.SaveChangesAsync(cancellationToken);

            string message = request.ForceDelete
                ? "Xóa vĩnh viễn từ vựng yêu thích thành công."
                : "Đã bỏ từ vựng khỏi danh sách yêu thích.";

            return OperationResult<bool>.Success(true, 200, message);
        }
    }
}