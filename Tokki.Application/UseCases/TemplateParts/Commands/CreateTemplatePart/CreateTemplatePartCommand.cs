using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart
{
    public class CreateTemplatePartCommand : IRequest<OperationResult<string>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public string PartTitle { get; set; } = string.Empty;
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? Instruction { get; set; }
        public int Mark { get; set; } 
        public string QuestionTypeId { get; set; } = string.Empty;
        public string? ExampleUrl { get; set; } 
    }
}