using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic
{
    public class CompleteTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = null!;

        [JsonIgnore]
        public string UserId { get; set; } = null!;
    }
}
