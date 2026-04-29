using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Commands.Delete
{
    public class DeleteTopikLevelConfigCommand : IRequest<OperationResult<bool>>
    {
        public int Id { get; set; }
    }
}
