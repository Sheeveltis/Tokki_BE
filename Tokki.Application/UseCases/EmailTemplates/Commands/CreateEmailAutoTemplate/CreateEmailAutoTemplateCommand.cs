using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailAutoTemplateCommand : IRequest<OperationResult<string>>
    {
        public string TemplateName { get; set; } = string.Empty;

        public EmailTemplateType Type { get; set; }

        public int Value { get; set; }

        public UserTargetGroup TargetGroup { get; set; } = UserTargetGroup.All;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
