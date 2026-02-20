using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; }
        public string FinishedWeekId { get; set; } 
    }
}