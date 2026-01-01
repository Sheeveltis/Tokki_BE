using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign
{

    public class DeleteEmailCampaignCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string? UpdateBy { get; set; }
        public string JobId { get; set; } = string.Empty;
    }
}
