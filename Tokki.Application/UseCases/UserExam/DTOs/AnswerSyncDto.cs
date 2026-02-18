using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class AnswerSyncDto
    {
        public string UserQuestionId { get; set; } = string.Empty;
        public string? SelectedOptionId { get; set; }
        public string? AnswerContent { get; set; }
    }
}
