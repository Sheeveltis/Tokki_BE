using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary
{
    public class AddFavoriteVocabularyCommandHandler
        : IRequestHandler<AddFavoriteVocabularyCommand, OperationResult<bool>>
    {
        private readonly IUserFavoriteVocabularyRepository _favoriteRepository;
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdGeneratorService _idGeneratorService;

        public AddFavoriteVocabularyCommandHandler(
            IUserFavoriteVocabularyRepository favoriteRepository,
            IVocabularyRepository vocabularyRepository,
            IHttpContextAccessor httpContextAccessor,
            IIdGeneratorService  idGeneratorService)
        {
            _favoriteRepository = favoriteRepository;
            _vocabularyRepository = vocabularyRepository;
            _httpContextAccessor = httpContextAccessor;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<bool>> Handle(AddFavoriteVocabularyCommand request, CancellationToken cancellationToken)
        {
           

            //  USER
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.Unauthorized },
                    401,
                    AppErrors.Unauthorized.Description
                );
            }

            //  VOCAB EXISTS
            var vocab = (await _vocabularyRepository.GetByIdsAsync(new List<string> { request.VocabularyId }))
     .FirstOrDefault();

            if (vocab == null || vocab.Status != Tokki.Domain.Enums.VocabularyStatus.Active)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404,
                    AppErrors.VocabularyNotFound.Description
                );
            }

            // IDEMPOTENT: ALREADY EXISTS -> SUCCESS
            var exists = await _favoriteRepository.ExistsAsync(userId, request.VocabularyId, cancellationToken);
            if (exists)
            {
                return OperationResult<bool>.Success(true, 200, "Đã tồn tại trong danh sách yêu thích.");
            }

            // 5) ADD
            try
            {
                var entity = new UserFavoriteVocabulary
                {
                    FavoriteVocabularyId = _idGeneratorService.GenerateCustom(15),
                    UserId = userId,
                    VocabularyId = request.VocabularyId,
                    CreatedAt = DateTime.UtcNow
                };

                await _favoriteRepository.AddAsync(entity, cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Thêm vào danh sách yêu thích thành công.");
            }
            catch
            {
                // nếu có unique index, race condition vẫn có thể xảy ra -> coi là đã tồn tại
                // nhưng để đúng format lỗi, trả Failure
                return OperationResult<bool>.Failure(
                    AppErrors.FavoriteVocabularyAddFailed,
                    400,
                    AppErrors.FavoriteVocabularyAddFailed.Description
                );
            }
        }
    }
}
