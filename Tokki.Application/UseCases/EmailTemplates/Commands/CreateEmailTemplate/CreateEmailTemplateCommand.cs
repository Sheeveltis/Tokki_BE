// CreateEmailTemplateCommand.cs
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailTemplateCommand : IRequest<OperationResult<string>> 
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}