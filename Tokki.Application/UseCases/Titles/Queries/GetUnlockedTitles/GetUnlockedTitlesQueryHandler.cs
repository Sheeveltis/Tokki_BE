using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using System.Collections.Generic;
using System.Linq;

namespace Tokki.Application.UseCases.Titles.Queries.GetUnlockedTitles
{
    public class GetUnlockedTitlesQueryHandler : IRequestHandler<GetUnlockedTitlesQuery, OperationResult<PagedResult<MyTitleResponse>>>
    {
        private readonly IUserTitleService _userTitleService;

        public GetUnlockedTitlesQueryHandler(IUserTitleService userTitleService)
        {
            _userTitleService = userTitleService;
        }

        public async Task<OperationResult<PagedResult<MyTitleResponse>>> Handle(GetUnlockedTitlesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _userTitleService.GetUnlockedTitlesWithPagingAsync(request.UserId, request.PageNumber, request.PageSize);
            
            var mappedItems = items.Select(x => new MyTitleResponse
            {
                TitleId = x.title.TitleId,
                Name = x.title.Name,
                Description = x.title.Description,
                ColorHex = x.title.ColorHex,
                IconUrl = x.title.IconUrl,
                RequirementType = x.title.RequirementType,
                RequirementQuantity = x.title.RequirementQuantity,
                EarnedAt = x.earnedAt
            }).ToList();

            var pagedResult = PagedResult<MyTitleResponse>.Create(mappedItems, totalCount, request.PageNumber, request.PageSize);
            
            return OperationResult<PagedResult<MyTitleResponse>>.Success(pagedResult, 200, "Lấy danh sách danh hiệu thành công.");
        }
    }
}
