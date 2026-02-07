using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff
{
    public class CreateQuestionBankByStaffCommand : IRequest<OperationResult<string>>
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
