using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamDetailStats
{
    public class GetExamDetailStatsQuery : IRequest<OperationResult<AdminExamStatsDTO>>
    {
        public string ExamId { get; set; } = string.Empty;

        public GetExamDetailStatsQuery(string examId)
        {
            ExamId = examId;
        }
    }
}
