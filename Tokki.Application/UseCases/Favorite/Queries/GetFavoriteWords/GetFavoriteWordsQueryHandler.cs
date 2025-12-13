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
using Tokki.Application.UseCases.Favorite.DTOs;

namespace Tokki.Application.UseCases.Favorite.Queries.GetFavoriteWords
{
    public class GetFavoriteWordsQueryHandler : IRequestHandler<GetFavoriteWordsQuery, OperationResult<PagedResult<FavoriteWordDto>>>
    {
        private readonly IUserFavoriteWordRepository _favoriteWordRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetFavoriteWordsQueryHandler(
            IUserFavoriteWordRepository favoriteWordRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _favoriteWordRepository = favoriteWordRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<PagedResult<FavoriteWordDto>>> Handle(
            GetFavoriteWordsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var (items, totalCount) = await _favoriteWordRepository.GetPagedByUserIdAsync(
                currentUserId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status
            );

            var dtos = items.Select(f => new FavoriteWordDto
            {
                FavoriteWordId = f.FavoriteWordId,
                WordId = f.WordId,
                WordText = f.Word.Text,
                Pronunciation = f.Word.Pronunciation,
                AudioURL = f.Word.AudioURL,
                MeaningId = f.MeaningId,
                Definition = f.Meaning?.Definition,
                Note = f.Note,
                CreateDate = f.CreateDate,
                Status = f.Status
            }).ToList();

            var pagedResult = PagedResult<FavoriteWordDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<FavoriteWordDto>>.Success(
                pagedResult,
                200,
                $"Tìm thấy {totalCount} từ vựng yêu thích."
            );
        }
    }
}
