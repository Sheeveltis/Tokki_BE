using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.DTOs;

namespace Tokki.Application.UseCases.Passages.Queries.GetPassages
{
    public class GetPassagesQueryHandler : IRequestHandler<GetPassagesQuery, OperationResult<PagedResult<PassageDto>>>
    {
        private readonly IPassageRepository _passageRepository;

        public GetPassagesQueryHandler(IPassageRepository passageRepository)
        {
            _passageRepository = passageRepository;
        }

        public async Task<OperationResult<PagedResult<PassageDto>>> Handle(GetPassagesQuery request, CancellationToken cancellationToken)
        {
            // ✅ null => repo không lọc status => lấy tất cả
            var (items, totalCount) = await _passageRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.MediaType,
                request.Status,
                cancellationToken
            );

            var dtos = items.Select(p => new PassageDto
            {
                PassageId = p.PassageId,
                Title = p.Title,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                AudioUrl = p.AudioUrl,
                MediaType = p.MediaType,
                CreatedAt = p.CreatedAt
            }).ToList();

            var paged = PagedResult<PassageDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<PassageDto>>.Success(paged, 200, $"Tìm thấy {totalCount} đoạn văn.");
        }
    }
}
