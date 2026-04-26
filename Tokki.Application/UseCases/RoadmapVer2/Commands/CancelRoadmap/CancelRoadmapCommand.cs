using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.RoadmapVer2.Commands.CancelRoadmap
{
    public class CancelRoadmapCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; } = string.Empty;
        public string RoadmapId { get; set; } = string.Empty;
    }
}
