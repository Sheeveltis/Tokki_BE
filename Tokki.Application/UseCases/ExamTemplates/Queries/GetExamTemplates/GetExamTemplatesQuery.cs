using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplates
{
    public class GetExamTemplatesQuery : IRequest<OperationResult<PagedResult<ExamTemplateDto>>>
    {
        public string? SearchTerm { get; set; }
        public ExamTemplateStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
