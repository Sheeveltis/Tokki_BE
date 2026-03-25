using System.Collections.Generic;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class ListeningDetailResponse
    {
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }

        public List<QuestionResultGroupDto> QuestionGroups { get; set; } = new();
    }

    public class ReadingDetailResponse
    {
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }

        public List<QuestionResultGroupDto> QuestionGroups { get; set; } = new();
    }

    public class QuestionResultGroupDto
    {
        public string? SharedMediaUrl { get; set; }
        public string SharedMediaType { get; set; } = "None";
        public string? SharedPassageContent { get; set; }

        public List<QuestionResultDto> Questions { get; set; } = new();
    }

    public class QuestionResultDto
    {
        public int QuestionNo { get; set; }
        public string Content { get; set; } = string.Empty;

        public string? SelectedOptionId { get; set; }
        public string? CorrectOptionId { get; set; }
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }

        public List<ExamOptionDto> Options { get; set; } = new();
    }
}