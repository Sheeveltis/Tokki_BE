using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById
{
    public class GetEmailAutoTemplateByIdQuery : IRequest<OperationResult<EmailTemplate>>
    {
        public string TemplateId { get; set; } = string.Empty;
    }
}
