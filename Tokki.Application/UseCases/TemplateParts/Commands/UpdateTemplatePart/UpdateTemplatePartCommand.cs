using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart
{
    public class UpdateTemplatePartCommand : IRequest<OperationResult<string>>
    {
        public string TemplatePartId { get; set; } = string.Empty;
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