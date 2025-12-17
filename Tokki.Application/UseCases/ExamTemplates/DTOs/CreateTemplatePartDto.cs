using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.DTOs
{
    public class CreateTemplatePartDto
    {
        public QuestionSkill Skill { get; set; } 
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? PartTitle { get; set; }
        public string? Instruction { get; set; }

        public DifficultyLevel DifficultyLevel { get; set; } 
        public string QuestionTypeId { get; set; } = string.Empty;

        public ExampleType ExampleType { get; set; } = ExampleType.None;
        public string? ExampleData { get; set; }
    }
}