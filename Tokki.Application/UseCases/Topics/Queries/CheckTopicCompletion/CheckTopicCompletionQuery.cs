using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.CheckTopicCompletion
{
    public class CheckTopicCompletionQuery : IRequest<OperationResult<TopicCompletionStatusDTO>>
    {
        public string TopicId { get; set; }
        [JsonIgnore]
        public string UserId { get; set; }
    }
}
