using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public record GetAlphabetDetailQuery(int Id) : IRequest<OperationResult<AlphabetDetailDto>>;
}
