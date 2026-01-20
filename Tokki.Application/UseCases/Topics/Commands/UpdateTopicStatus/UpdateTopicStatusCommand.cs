using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus
{
    public class UpdateTopicStatusCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;

        public TopicStatus Status { get; set; }

        [JsonIgnore]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
