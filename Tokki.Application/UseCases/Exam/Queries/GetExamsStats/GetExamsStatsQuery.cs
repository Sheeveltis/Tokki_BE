using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamsStats
{
    public class GetExamsStatsQuery : IRequest<OperationResult<PagedResult<AdminExamStatsDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public ExamStatus? Status { get; set; }
        public ExamType? Type { get; set; }
        public ExamCreatorFilter CreatorFilter { get; set; } = ExamCreatorFilter.All;
    }
}
