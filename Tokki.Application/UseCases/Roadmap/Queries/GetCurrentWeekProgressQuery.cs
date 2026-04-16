using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetCurrentWeekProgress
{
    public class GetCurrentWeekProgressQuery : IRequest<OperationResult<CurrentWeekProgressViewModel>>
    {
        public string UserId { get; set; }

        public GetCurrentWeekProgressQuery(string userId)
        {
            UserId = userId;
        }
    }
}
