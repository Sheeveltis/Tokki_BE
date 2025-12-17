using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.TemplateParts.CreateTemplatePart
{
    public class CreateTemplatePartCommand : IRequest<OperationResult<string>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public string PartTitle { get; set; } = string.Empty;

        public QuestionSkill Skill { get; set; } 

        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? Instruction { get; set; }

        public DifficultyLevel DifficultyLevel { get; set; } 
        public string QuestionTypeId { get; set; } = string.Empty;

        public ExampleType ExampleType { get; set; }
        public string? ExampleData { get; set; }
    }
}