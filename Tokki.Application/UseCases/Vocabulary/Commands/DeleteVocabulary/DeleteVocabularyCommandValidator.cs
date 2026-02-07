using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary
{
    public class DeleteVocabularyCommandValidator : AbstractValidator<DeleteVocabularyCommand>
    {
        public DeleteVocabularyCommandValidator()
        {
            RuleFor(x => x.VocabularyId)
                .NotEmpty()
                .WithName("VocabularyId ");

        }
    }
}
