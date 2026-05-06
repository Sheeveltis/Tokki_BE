using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Alphabet.Commands.UpdateAlphabet
{
    public class UpdateAlphabetCommand : IRequest<OperationResult<bool>>
    {
        public int Id { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? Pronunciation { get; set; }
        public AlphabetType Type { get; set; }
        public string? AudioUrl { get; set; }
        public string? DisplayDataJson { get; set; }
        public string? ValidationDataJson { get; set; }
        public int TotalStrokes { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
