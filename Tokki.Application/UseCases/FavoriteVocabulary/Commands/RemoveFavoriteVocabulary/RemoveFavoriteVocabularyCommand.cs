using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary
{
    public class RemoveFavoriteVocabularyCommand : IRequest<OperationResult<bool>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
