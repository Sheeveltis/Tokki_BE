using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommand : IRequest<OperationResult<GenerateNextWeekResult>>
    {
        public string UserId { get; set; }
        public string FinishedWeekId { get; set; }
    }

    public class GenerateNextWeekResult
    {
        public bool IsGenerated { get; set; }
        public bool HasWarning { get; set; }
        public string? WarningMessage { get; set; }
        public List<string> PersistentWeakTypeIds { get; set; } = new();
    }
}