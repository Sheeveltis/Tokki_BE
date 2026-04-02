using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class AdminExamStatsDTO : AdminExamDTO
    {
        public int TotalParticipants { get; set; }
        public double AverageScore { get; set; }
        public int PdfDownloadCount { get; set; }
        public int TopScore { get; set; }
        public double AverageDurationMinutes { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }

        public Dictionary<string, int> SkillQuestionCounts { get; set; } = new();
    }
}
