using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create
{
    public class CreateQuestionOptionCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string QuestionBankId { get; set; } = string.Empty;

        public string KeyOption { get; set; } = string.Empty; // "1".."4"
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCorrect { get; set; }
    }
}
