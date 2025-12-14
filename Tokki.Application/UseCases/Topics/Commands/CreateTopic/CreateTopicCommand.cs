using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopic
{
    public class CreateTopicCommand : IRequest<OperationResult<string>>
    {
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        [JsonIgnore]
        public string CreateBy { get; set; } = string.Empty;
    }
}
