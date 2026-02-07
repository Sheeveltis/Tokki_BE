
using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank
{
    public class CreateQuestionBankCommand : IRequest<OperationResult<string>>
    {
        public string? PassageId { get; set; }
        public string? QuestionTypeId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string? Explanation { get; set; }
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
        [JsonIgnore]
        public string? CreateBy { get; set; }
    }
}
