using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class UserExamReviewResponse
    {
        public string UserExamId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public int TimeSpentMinutes { get; set; }
        public DateTime? SubmitTime { get; set; }
        public List<ReviewQuestionDto> Questions { get; set; } = new();
    }

    public class ReviewQuestionDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public string Skill { get; set; } = string.Empty;
        public double QuestionMaxScore { get; set; }
        public List<ReviewOptionDto>? Options { get; set; }
        public string? SelectedOptionId { get; set; }
        public bool? IsCorrect { get; set; }

        public string? WritingAnswerContent { get; set; }
        public int? WritingScore { get; set; }
        public string? AiAnalysisJson { get; set; }
    }

    public class ReviewOptionDto
    {
        public string OptionId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
