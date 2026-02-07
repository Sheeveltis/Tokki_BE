
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank
{
    public class DeleteQuestionBankCommand : IRequest<OperationResult<bool>>
    {
        public string QuestionBankId { get; set; } = string.Empty;
    }
}
