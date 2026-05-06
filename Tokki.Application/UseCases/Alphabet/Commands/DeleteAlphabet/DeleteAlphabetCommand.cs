using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Alphabet.Commands.DeleteAlphabet
{
    public record DeleteAlphabetCommand(int Id) : IRequest<OperationResult<bool>>;
}
