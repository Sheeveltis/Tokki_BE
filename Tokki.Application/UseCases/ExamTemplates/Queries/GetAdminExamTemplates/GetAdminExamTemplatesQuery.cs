using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates
{
    public class GetAdminExamTemplatesQuery : IRequest<OperationResult<PagedResult<AdminExamTemplateDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public ExamTemplateStatus? Status { get; set; }
        public ExamType? Type { get; set; }
        public ExamCreatorFilter? CreatorFilter { get; set; } = ExamCreatorFilter.All;
    }
}