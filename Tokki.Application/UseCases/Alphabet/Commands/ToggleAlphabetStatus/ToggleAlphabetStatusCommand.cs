using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Alphabet.Commands.ToggleAlphabetStatus
{
    public record ToggleAlphabetStatusCommand(int Id) : IRequest<OperationResult<bool>>;
}
