using MediatR;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval
{
    public class SubmitQuestionBankForApprovalCommand : IRequest<OperationResult<List<string>>>
    {
        public List<string> QuestionBankIds { get; set; } = new();

        // set từ Controller (token), không nhận từ body
        [JsonIgnore]
        public string? SubmittedBy { get; set; }
    }
}
