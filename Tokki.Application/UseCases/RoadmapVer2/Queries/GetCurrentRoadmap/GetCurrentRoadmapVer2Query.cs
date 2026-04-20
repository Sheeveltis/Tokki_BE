using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.RoadmapVer2.DTOs;

namespace Tokki.Application.UseCases.RoadmapVer2.Queries.GetCurrentRoadmap
{
    public class GetCurrentRoadmapVer2Query : IRequest<OperationResult<CurrentRoadmapVer2ViewModel>>
    {
        public string UserId { get; set; }

        public GetCurrentRoadmapVer2Query(string userId)
        {
            UserId = userId;
        }
    }
}
