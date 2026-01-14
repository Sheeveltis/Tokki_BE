using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank
{
    public class ApproveQuestionBanksCommand : IRequest<OperationResult<List<string>>>
    {
        public List<string> QuestionBankIds { get; set; } = new();
    }
}
