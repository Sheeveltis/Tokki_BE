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
using Tokki.Application.UseCases.FavoriteVocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies
{
    public class GetFavoriteVocabulariesQueryHandler
       : IRequestHandler<GetFavoriteVocabulariesQuery, OperationResult<PagedResult<FavoriteVocabularyDto>>>
    {
        private readonly IUserFavoriteVocabularyRepository _favoriteRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetFavoriteVocabulariesQueryHandler(
            IUserFavoriteVocabularyRepository favoriteRepository,
            ITopicRepository topicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _favoriteRepository = favoriteRepository;
            _topicRepository = topicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<PagedResult<FavoriteVocabularyDto>>> Handle(GetFavoriteVocabulariesQuery request, CancellationToken cancellationToken)
        {
            // USER
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return OperationResult<PagedResult<FavoriteVocabularyDto>>.Failure(
                    new List<Error> { AppErrors.Unauthorized },
                    401,
                    AppErrors.Unauthorized.Description
                );
            }

            var topicId = string.IsNullOrWhiteSpace(request.TopicId) ? null : request.TopicId.Trim();

            // Nếu có TopicId thì check topic tồn tại (tuỳ bạn có muốn check hay không)
            if (topicId != null)
            {
                var topic = await _topicRepository.GetByIdAsync(topicId);
                if (topic == null || topic.Status != TopicStatus.Active)
                {
                    return OperationResult<PagedResult<FavoriteVocabularyDto>>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }
            }

            // GET PAGED
            var (items, totalCount) = await _favoriteRepository.GetPagedByUserAndTopicAsync(
                userId: userId,
                topicId: topicId,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                cancellationToken: cancellationToken);

            var dtos = items.Select(x => new FavoriteVocabularyDto
            {
                VocabularyId = x.VocabularyId,
                Text = x.Vocabulary.Text,
                Definition = x.Vocabulary.Definition,
                Pronunciation = x.Vocabulary.Pronunciation,
                ImgURL = x.Vocabulary.ImgURL,
                AudioURL = x.Vocabulary.AudioURL,
                FavoritedAt = x.CreatedAt
            }).ToList();

            var paged = PagedResult<FavoriteVocabularyDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<FavoriteVocabularyDto>>.Success(
                paged,
                200,
                "Lấy danh sách từ vựng yêu thích thành công"
            );
        }
    }
}
