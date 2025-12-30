using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign
{

    public class DeleteEmailCampaignCommand : IRequest<OperationResult<string>>
    {
        public string JobId { get; set; } = string.Empty;
    }
}
