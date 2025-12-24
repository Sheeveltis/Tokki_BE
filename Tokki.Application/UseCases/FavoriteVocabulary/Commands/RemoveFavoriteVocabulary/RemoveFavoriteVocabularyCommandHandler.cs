using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary;

namespace Tokki.Application.UseCases.UserFavoriteVocabularies.Commands.RemoveFavoriteVocabulary
{
    public class RemoveFavoriteVocabularyCommandHandler
        : IRequestHandler<RemoveFavoriteVocabularyCommand, OperationResult<bool>>
    {
        private readonly IUserFavoriteVocabularyRepository _favoriteRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<RemoveFavoriteVocabularyCommand> _validator;

        public RemoveFavoriteVocabularyCommandHandler(
            IUserFavoriteVocabularyRepository favoriteRepository,
            IHttpContextAccessor httpContextAccessor,
            IValidator<RemoveFavoriteVocabularyCommand> validator)
        {
            _favoriteRepository = favoriteRepository;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<OperationResult<bool>> Handle(RemoveFavoriteVocabularyCommand request, CancellationToken cancellationToken)
        {
            // 1) VALIDATION
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                return OperationResult<bool>.Failure(errors, 400, AppErrors.ValidationFailed.Description);
            }

            // 2) USER
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.Unauthorized },
                    401,
                    AppErrors.Unauthorized.Description
                );
            }

            // 3) HARD DELETE (idempotent: không có vẫn success)
            try
            {
                var deleted = await _favoriteRepository.HardDeleteAsync(userId, request.VocabularyId, cancellationToken);

                if (deleted == 0)
                {
                    return OperationResult<bool>.Success(true, 200, "Từ vựng không tồn tại trong danh sách yêu thích.");
                }

                return OperationResult<bool>.Success(true, 200, "Gỡ khỏi danh sách yêu thích thành công.");
            }
            catch
            {
                return OperationResult<bool>.Failure(
                    AppErrors.FavoriteVocabularyRemoveFailed,
                    400,
                    AppErrors.FavoriteVocabularyRemoveFailed.Description
                );
            }
        }
    }
}
