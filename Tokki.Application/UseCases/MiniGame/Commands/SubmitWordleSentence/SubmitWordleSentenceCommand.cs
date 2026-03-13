using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleSentence
{
    public class SubmitWordleSentenceCommand : IRequest<OperationResult<WordleSubmissionResponse>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        public string DailyWordleId { get; set; } = string.Empty;
        public string SentenceContent { get; set; } = string.Empty;

    }
}
