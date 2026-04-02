using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetPagedTitles
{
    public class GetPagedTitlesQueryHandler : IRequestHandler<GetPagedTitlesQuery, OperationResult<PagedResult<Title>>>
    {
        private readonly ITitleRepository _titleRepository;

        public GetPagedTitlesQueryHandler(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public async Task<OperationResult<PagedResult<Title>>> Handle(GetPagedTitlesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _titleRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                request.RequirementType,
                cancellationToken);

            var pagedResult = PagedResult<Title>.Create(items, totalCount, request.PageNumber, request.PageSize);
            return OperationResult<PagedResult<Title>>.Success(pagedResult, 200, "Lấy danh sách danh hiệu thành công.");
        }
    }
}
