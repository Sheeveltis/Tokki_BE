using FluentValidation;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.DeleteExample
{
    public class DeleteVocabularyExampleCommandValidator : AbstractValidator<DeleteVocabularyExampleCommand>
    {
        public DeleteVocabularyExampleCommandValidator()
        {
            RuleFor(x => x.ExampleId)
                .NotEmpty()
                .WithName("ExampleId");
        }
    }
}
