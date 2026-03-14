using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex
{
    public class UpdateTopicOrderIndexCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
        public int OrderIndex { get; set; } // vị trí muốn set (>=1)

        [JsonIgnore]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}