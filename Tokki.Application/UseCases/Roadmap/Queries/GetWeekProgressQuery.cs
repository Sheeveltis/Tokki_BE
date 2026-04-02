using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Queries
{
    public class GetWeekProgressQuery : IRequest<OperationResult<int>>
    {
        public string RoadmapWeekId { get; set; } = string.Empty;
    }
}
