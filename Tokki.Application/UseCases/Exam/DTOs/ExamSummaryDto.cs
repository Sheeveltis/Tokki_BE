using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExamSummaryDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ExamType Type { get; set; }
        public ExamStatus Status { get; set; }
        public string ExamTemplateName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int CompletedQuestions { get; set; }
        public int Progress { get; set; } // Percentage
    }
}
