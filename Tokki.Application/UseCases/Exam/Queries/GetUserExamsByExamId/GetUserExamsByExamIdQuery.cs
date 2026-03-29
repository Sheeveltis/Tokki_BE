using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetUserExamsByExamId
{
    public class GetUserExamsByExamIdQuery : IRequest<OperationResult<PagedResult<ExamParticipantDTO>>>
    {
        public string ExamId { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public ExamParticipantSortBy SortBy { get; set; } = ExamParticipantSortBy.SubmitTime;
        public bool IsDescending { get; set; } = true;
    }
}
