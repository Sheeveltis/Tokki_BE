using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.TopikWriting.DTOs
{
    public sealed class Question51Data
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Status { get; set; }
        public string QuestionTypeId { get; set; } = string.Empty;
        public int Skill { get; set; }   // từ QuestionTypes
    }
}
