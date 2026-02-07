using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff
{
    public class CreateTopicByStaffCommand : IRequest<OperationResult<string>>
    {
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TopicLevel Level { get; set; }
        public string? ImgUrl { get; set; }
    }
}
