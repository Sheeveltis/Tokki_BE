using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExamStatProjection
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamTemplateId { get; set; } = string.Empty;
        public string? ExamTemplateName { get; set; }
        public string Title { get; set; } = string.Empty;
        public ExamType Type { get; set; }
        public ExamStatus Status { get; set; }
        public int Duration { get; set; }
        public string? SkillDurations { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PdfDownloadCount { get; set; }

        public int TotalParticipants { get; set; }
        public double AverageScore { get; set; }
        public int TopScore { get; set; }
        public double AverageDurationMinutes { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalQuestions { get; set; }
        public int MaxScore { get; set; }

        public List<TemplatePartStatProjection> TemplateParts { get; set; } = new();
        public List<int> QuestionNumbers { get; set; } = new();
    }

    public class TemplatePartStatProjection
    {
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public int Mark { get; set; }
    }
}
