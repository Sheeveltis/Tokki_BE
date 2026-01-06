using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update
{
    public class UpdateQuestionOptionCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string QuestionBankId { get; set; } = string.Empty;

        [JsonIgnore]
        public string OptionId { get; set; } = string.Empty;
        public string? KeyOption { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsCorrect { get; set; }
    }
}
