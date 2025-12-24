using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary
{
    public class AddFavoriteVocabularyCommandValidator : AbstractValidator<AddFavoriteVocabularyCommand>
    {
        public AddFavoriteVocabularyCommandValidator()
        {
            RuleFor(x => x.VocabularyId)
                .NotEmpty()
                .WithName("VocabularyId");
        }
    }
}
