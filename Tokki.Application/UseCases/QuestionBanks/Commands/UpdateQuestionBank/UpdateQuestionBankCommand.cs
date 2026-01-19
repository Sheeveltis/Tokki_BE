using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommand : IRequest<OperationResult<string>>
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string? PassageId { get; set; }
        public string? QuestionTypeId { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? Explanation { get; set; }
        public List<CreateQuestionOptionDto>? Options { get; set; }
    }
}
