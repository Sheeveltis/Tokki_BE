using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateTemplatePart
{
    public class UpdateExamTemplatePartCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore] 
        public string TemplatePartId { get; set; } = string.Empty;

        public string ExamTemplateId { get; set; } = string.Empty;

        public string? PartTitle { get; set; }
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? Instruction { get; set; }
        public int Mark { get; set; }
        public string QuestionTypeId { get; set; } = string.Empty;
        public string? ExampleUrl { get; set; }
    }
}