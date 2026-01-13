using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

public class GetQuestionBanksByQuestionTypeIdQuery
        : IRequest<OperationResult<List<QuestionBankByQuestionTypeDto>>>
{
    public string QuestionTypeId { get; set; } = string.Empty;

    public QuestionBankStatus? Status { get; set; }
}