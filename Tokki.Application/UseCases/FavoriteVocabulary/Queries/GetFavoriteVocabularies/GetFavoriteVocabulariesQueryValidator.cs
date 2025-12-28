using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies
{
    public class GetFavoriteVocabulariesQueryValidator : AbstractValidator<GetFavoriteVocabulariesQuery>
    {
        public GetFavoriteVocabulariesQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithName("PageNumber");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithName("PageSize");
        }
    }

}
