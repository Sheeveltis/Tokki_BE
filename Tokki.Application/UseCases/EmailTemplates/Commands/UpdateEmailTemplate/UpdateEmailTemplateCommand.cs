using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate
{
    public class UpdateEmailTemplateCommand : IRequest<OperationResult<string>>
    {
        public string TemplateId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}