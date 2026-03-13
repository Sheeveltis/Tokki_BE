using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceExam
{
    public class GetEntranceExamQuery : IRequest<OperationResult<EntranceExamResult>>
    {
        public TargetAimLevel TargetAim { get; set; }
    }

    public class EntranceExamResult
    {
        public string ExamId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string ExamGroup { get; set; } = string.Empty; 
        public int PassScore { get; set; }
        public int TotalScore { get; set; }
    }
}