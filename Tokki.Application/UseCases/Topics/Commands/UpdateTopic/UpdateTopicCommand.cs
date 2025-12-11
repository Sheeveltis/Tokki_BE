using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopic
{
    public class UpdateTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public TopicStatus Status { get; set; }
    }
}
