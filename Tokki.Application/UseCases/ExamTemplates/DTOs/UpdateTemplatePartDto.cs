using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.DTOs
{
    public class UpdateTemplatePartDto
    {
        public string? TemplatePartId { get; set; } 
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? PartTitle { get; set; }
        public string? Instruction { get; set; }
        public int Mark { get; set; } 
        public string QuestionTypeId { get; set; } = string.Empty;
        public string? ExampleUrl { get; set; } 
    }
}