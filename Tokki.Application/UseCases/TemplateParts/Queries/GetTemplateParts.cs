using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.TemplateParts.Queries.GetTemplateParts
{
    public class GetTemplatePartsQuery : IRequest<OperationResult<PagedResult<TemplatePartDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? ExamTemplateId { get; set; }
    }
}