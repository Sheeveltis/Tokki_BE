using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplateStatus
{
    public class UpdateExamTemplateStatusCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string ExamTemplateId { get; set; } = string.Empty;

        public ExamTemplateStatus Status { get; set; }
    }
}