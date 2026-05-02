using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikLevelConfigs.DTOs;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetById
{
    public class GetTopikLevelConfigByIdQuery : IRequest<OperationResult<TopikLevelConfigDto>>
    {
        public int Id { get; set; }
    }
}
