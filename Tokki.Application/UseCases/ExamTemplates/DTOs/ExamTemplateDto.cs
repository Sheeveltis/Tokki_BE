using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.DTOs
{
    public class ExamTemplateDto
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ExamType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public ExamTemplateStatus Status { get; set; }
        public int TotalParts { get; set; }
        public int TotalQuestions { get; set; }
        public List<TemplatePartDto> Parts { get; set; } = new();
    }
}