using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class AdminExamDTO
    {
        public string ExamId { get; set; } = default!;
        public string ExamTemplateId { get; set; } = default!;
        public string? ExamTemplateName { get; set; } 
        public string Title { get; set; } = default!;
        public ExamType Type { get; set; }
        public ExamStatus? Status { get; set; }
        public int Duration { get; set; }
        public Dictionary<string, int> SkillDurations { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int MaxScore { get; set; }
        public Dictionary<string, int> SkillTotalScores { get; set; } = new();
    }
}
