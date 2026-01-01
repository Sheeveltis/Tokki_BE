using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaignById
{
    public class GetEmailCampaignByIdQuery : IRequest<OperationResult<EmailJob>>
    {
        public string JobId { get; set; } = string.Empty;
    }
}
