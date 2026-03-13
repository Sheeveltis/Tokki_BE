using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class WritingDetailResponse
    {
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public int TotalQuestions { get; set; }

        public List<QuestionWritingResultGroupDto> QuestionGroups { get; set; } = new();
    }

    public class QuestionWritingResultGroupDto
    {
        public string? SharedMediaUrl { get; set; }
        public string SharedMediaType { get; set; } = "None";
        public string? SharedPassageContent { get; set; }

        public List<QuestionWritingResultDto> Questions { get; set; } = new();
    }

    public class QuestionWritingResultDto
    {
        public int QuestionNo { get; set; }
        public string Content { get; set; } = string.Empty;

        public string? AnswerContent { get; set; }
        public int? WordCount { get; set; }
        public double? Score { get; set; }
        public object? AiAnalysis { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
