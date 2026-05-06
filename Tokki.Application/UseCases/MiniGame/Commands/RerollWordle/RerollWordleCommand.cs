using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.MiniGame.Commands.RerollWordle
{
    public class RerollWordleCommand : IRequest<OperationResult<bool>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
    }
}
