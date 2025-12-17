using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetById
{
    public class GetVocabularyDetailByIdQueryValidator
        : AbstractValidator<GetVocabularyDetailByIdQuery>
    {
        public GetVocabularyDetailByIdQueryValidator()
        {
            RuleFor(x => x.VocabularyId)
                .NotEmpty()
                .WithName("VocabularyId");
        }
    }
}
