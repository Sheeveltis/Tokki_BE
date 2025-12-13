
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById
{
    public class GetQuestionBankByIdQuery : IRequest<OperationResult<QuestionBankDto>>
    {
        public string QuestionBankId { get; set; } = string.Empty;
    }
}
