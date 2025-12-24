using FluentValidation;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary
{
    public class RemoveFavoriteVocabularyCommandValidator : AbstractValidator<RemoveFavoriteVocabularyCommand>
    {
        public RemoveFavoriteVocabularyCommandValidator()
        {
            RuleFor(x => x.VocabularyId)
                .NotEmpty()
                .WithName("VocabularyId");
        }
    }
}
