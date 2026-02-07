using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.TemplateParts.Queries.GetTemplatePartById
{
    public class GetTemplatePartByIdQuery : IRequest<OperationResult<TemplatePartDto>>
    {
        public string TemplatePartId { get; set; } = string.Empty;
    }
}