using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.DTOs
{
    public class TemplatePartDto
    {
        public string TemplatePartId { get; set; } = string.Empty;
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? PartTitle { get; set; }
        public string? Instruction { get; set; }
        public int Mark { get; set; }
        public string? ExampleUrl { get; set; }
        public string QuestionTypeId { get; set; } = string.Empty;
        public string QuestionTypeName { get; set; } = string.Empty; 
    }
}