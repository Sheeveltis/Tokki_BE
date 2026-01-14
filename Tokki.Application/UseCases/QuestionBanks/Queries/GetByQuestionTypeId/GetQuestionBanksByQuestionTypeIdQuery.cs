using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId
{
    public class GetQuestionBanksByQuestionTypeIdQuery
        : IRequest<OperationResult<List<QuestionBankByQuestionTypeDto>>>
    {
        public string QuestionTypeId { get; set; } = string.Empty;

        public QuestionBankStatus? Status { get; set; }

        // NEW: filter theo audit
        public string? CreateBy { get; set; }
        public string? ApprovedBy { get; set; }
    }
}
