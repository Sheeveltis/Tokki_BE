using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class QuestionBankByQuestionTypeDto
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string? PassageId { get; set; }
        public string? PassageTitle { get; set; }
        public string? QuestionTypeId { get; set; }
        public string? QuestionTypeName { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? Explanation { get; set; }
        public QuestionBankStatus Status { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }
}
