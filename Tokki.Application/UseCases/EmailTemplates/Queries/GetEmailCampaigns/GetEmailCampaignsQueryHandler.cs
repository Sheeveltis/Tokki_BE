using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaigns
{

    public class GetEmailCampaignsQueryHandler
        : IRequestHandler<GetEmailCampaignsQuery, OperationResult<PagedResult<EmailJob>>>
    {
        private readonly IEmailJobRepository _repo;

        public GetEmailCampaignsQueryHandler(IEmailJobRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<PagedResult<EmailJob>>> Handle(GetEmailCampaignsQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await _repo.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.Status,
                request.TargetGroup,
                request.ScheduledFrom,
                request.ScheduledTo,
                request.CreatedFrom,
                request.CreatedTo,
                request.SearchSubject,
                request.IncludeDeleted
            );

            var paged = PagedResult<EmailJob>.Create(items, total, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<EmailJob>>.Success(paged, 200, "Lấy danh sách campaign thành công");
        }
    }
}
