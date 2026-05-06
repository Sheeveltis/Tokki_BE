using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetTargetLevelByScore
{
    public class GetTargetLevelByScoreQuery : IRequest<OperationResult<TargetLevelDto>>
    {
        public int Score { get; set; }
        public int ExamGroup { get; set; } // 1 for TOPIK I, 2 for TOPIK II
    }

    public class TargetLevelDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public int ExamGroup { get; set; }
        public int TargetAimLevel { get; set; }
    }
}
