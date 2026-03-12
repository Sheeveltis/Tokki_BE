using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.UserExam.Commands.SyncWritingProgress
{
    public class SyncWritingProgressCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
        public string UserQuestionId { get; set; } = string.Empty;
        public string? AnswerContent { get; set; }
    }
}
