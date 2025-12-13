using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Favorite.DTOs;
using Tokki.Domain.Enums;
namespace Tokki.Application.UseCases.Favorite.Queries.GetFavoriteTopics
{
    public class GetFavoriteTopicsQueryHandler : IRequestHandler<GetFavoriteTopicsQuery, OperationResult<PagedResult<FavoriteTopicDto>>>
    {
        private readonly IUserFavoriteTopicRepository _favoriteTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public GetFavoriteTopicsQueryHandler(
            IUserFavoriteTopicRepository favoriteTopicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _favoriteTopicRepository = favoriteTopicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<PagedResult<FavoriteTopicDto>>> Handle(
            GetFavoriteTopicsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var (items, totalCount) = await _favoriteTopicRepository.GetPagedByUserIdAsync(
                currentUserId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status
            );

            var dtos = items.Select(f => new FavoriteTopicDto
            {
                FavoriteTopicId = f.FavoriteTopicId,
                TopicId = f.TopicId,
                TopicName = f.Topic.TopicName,
                Description = f.Topic.Description,
                Note = f.Note,
                WordCount = f.Topic.MeaningTopics.Count(mt => mt.Status == MeaningTopicStatus.Active),
                CreateDate = f.CreateDate,
                Status = f.Status
            }).ToList();

            var pagedResult = PagedResult<FavoriteTopicDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<FavoriteTopicDto>>.Success(
                pagedResult,
                200,
                $"Tìm thấy {totalCount} chủ đề yêu thích."
            );
        }
    }
}