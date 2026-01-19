using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank
{
    public class RejectQuestionBanksCommand : IRequest<OperationResult<List<string>>>
    {
        public List<string> QuestionBankIds { get; set; } = new();
        public string RejectReason { get; set; } = string.Empty;
    }
}
