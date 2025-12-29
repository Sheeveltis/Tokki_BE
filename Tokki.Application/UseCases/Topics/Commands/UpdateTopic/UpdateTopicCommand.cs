using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopic
{
    public class UpdateTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;

        // Cho phép null => client không gửi thì không update
        public string? TopicName { get; set; }

        public string? Description { get; set; }

        // Nullable để biết client có gửi hay không
        public TopicLevel? Level { get; set; }

        // Nullable để biết client có gửi hay không
        public TopicStatus? Status { get; set; }

        public string? ImgUrl { get; set; }

        [JsonIgnore]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
