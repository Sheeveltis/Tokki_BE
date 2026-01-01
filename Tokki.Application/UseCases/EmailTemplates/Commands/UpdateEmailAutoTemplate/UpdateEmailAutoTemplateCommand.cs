using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate
{
    public class UpdateEmailAutoTemplateCommand : IRequest<OperationResult<string>>
    {
        public string TemplateId { get; set; } = string.Empty;

        // Optional: nếu null/"" => không update
        public string? TemplateName { get; set; }

        public EmailTemplateType? Type { get; set; }

        public int? Value { get; set; }

        public UserTargetGroup? TargetGroup { get; set; }

        public EmailTemplateStatus? Status { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public string? Description { get; set; }
    }
}
