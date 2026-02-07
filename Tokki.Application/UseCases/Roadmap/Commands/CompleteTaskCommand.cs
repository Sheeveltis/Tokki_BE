using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.CompleteTask
{
    public class CompleteTaskCommand : IRequest<OperationResult<bool>>
    {
        public string TaskId { get; set; }
        public string UserId { get; set; }
        public string? Performance { get; set; }
    }
}