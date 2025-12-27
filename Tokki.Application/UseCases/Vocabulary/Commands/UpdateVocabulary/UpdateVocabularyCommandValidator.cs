using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary
{
    public class UpdateVocabularyCommandValidator : AbstractValidator<UpdateVocabularyCommand>
    {
        public UpdateVocabularyCommandValidator()
        {
            RuleFor(x => x.VocabularyId)
                .NotEmpty() 
                .WithName("VocabularyId");

            RuleFor(x => x.UpdateData.Definition)
                .MaximumLength(500) 
                .WithName("Definition")
                .When(x => x.UpdateData.Definition != null);

           
        }
    }
}