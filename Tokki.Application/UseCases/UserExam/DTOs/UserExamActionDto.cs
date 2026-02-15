using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class UserExamActionDto
    {
        public string UserExamId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;

        public double? TotalScore { get; set; }
        public double? MaxScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastAttempt { get; set; }
        public int TimeRemaining { get; set; }
    }
}
