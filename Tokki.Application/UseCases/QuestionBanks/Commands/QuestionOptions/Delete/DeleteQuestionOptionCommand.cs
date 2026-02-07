using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete
{
    public class DeleteQuestionOptionCommand : IRequest<OperationResult<bool>>
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string OptionId { get; set; } = string.Empty;
    }
}
