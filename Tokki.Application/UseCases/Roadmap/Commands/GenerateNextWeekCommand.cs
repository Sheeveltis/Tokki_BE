using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommand : IRequest<OperationResult<string>>
    {
        public string UserId { get; set; } = string.Empty;
        public string FinishedWeekId { get; set; } = string.Empty;
    }
}