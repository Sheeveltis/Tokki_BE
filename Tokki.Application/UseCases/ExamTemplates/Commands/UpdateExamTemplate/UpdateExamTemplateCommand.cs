using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string ExamTemplateId { get; set; } = string.Empty;

        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ExamType? Type { get; set; }
        public ExamTemplateStatus? Status { get; set; }
    }
}