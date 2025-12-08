using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries
{
    public class GetEmailTemplateByIdQuery : IRequest<OperationResult<EmailTemplate>>
    {
        public int TemplateId { get; set; }
    }
}