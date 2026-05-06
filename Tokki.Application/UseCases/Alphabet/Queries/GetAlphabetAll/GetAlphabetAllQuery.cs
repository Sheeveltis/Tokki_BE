using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public class GetAlphabetAllQuery : IRequest<OperationResult<List<AlphabetDto>>>
    {
        public AlphabetType? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}
