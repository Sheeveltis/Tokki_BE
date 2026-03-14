using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.ProcessWeeklyResult
{
    public class ProcessWeeklyResultCommand : IRequest<OperationResult<ProcessWeeklyResultData>>
    {
        public string UserId { get; set; } = string.Empty;
        public string UserExamId { get; set; } = string.Empty;
    }

    public class ProcessWeeklyResultData
    {
        public int ScorePercent { get; set; }
        public List<string> WeakTypeIds { get; set; } = new();
        public List<string> PersistentWeakTypeIds { get; set; } = new();
        public bool HasWarning { get; set; }
    }
}