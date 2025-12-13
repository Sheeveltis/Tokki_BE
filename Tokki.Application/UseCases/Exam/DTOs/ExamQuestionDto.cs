using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExamQuestionDto
    {
        public string ExamQuestionId { get; set; } = string.Empty;
        public int QuestionNo { get; set; }
        public int Score { get; set; }
        public string QuestionBankId { get; set; } = string.Empty;
        public QuestionBankDto? QuestionBank { get; set; }
    }
}
