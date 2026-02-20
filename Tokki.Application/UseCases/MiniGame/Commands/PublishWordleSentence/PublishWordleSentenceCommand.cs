using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.MiniGame.Commands.PublishWordleSentence
{
    public class PublishWordleSentenceCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        public string SubmissionId { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
