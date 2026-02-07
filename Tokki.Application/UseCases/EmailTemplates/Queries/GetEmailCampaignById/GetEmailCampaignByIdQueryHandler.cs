using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaignById
{
    public class GetEmailCampaignByIdQueryHandler
         : IRequestHandler<GetEmailCampaignByIdQuery, OperationResult<EmailJob>>
    {
        private readonly IEmailJobRepository _repo;

        public GetEmailCampaignByIdQueryHandler(IEmailJobRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<EmailJob>> Handle(GetEmailCampaignByIdQuery request, CancellationToken cancellationToken)
        {
            var job = await _repo.GetByIdAsync(request.JobId);
            if (job == null)
                return OperationResult<EmailJob>.Failure("Không tìm thấy campaign!", 404);

            return OperationResult<EmailJob>.Success(job, 200);
        }
    }

}
