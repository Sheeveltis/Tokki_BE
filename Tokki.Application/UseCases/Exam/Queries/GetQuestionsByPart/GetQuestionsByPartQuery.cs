using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart
{
    public class GetQuestionsByPartQuery : IRequest<OperationResult<PagedResult<AvailableQuestionDTO>>>
    {
        public string TemplatePartId { get; set; } = default!;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}
