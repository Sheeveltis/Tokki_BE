using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap
{
    public class GetRoadmapQuery : IRequest<OperationResult<RoadmapViewModel>>
    {
        public string UserId { get; set; }

        public GetRoadmapQuery(string userId)
        {
            UserId = userId;
        }
    }
}