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

            RuleFor(x => x.UpdateData.Text)
                .MaximumLength(200)
                .WithName("Text")
                .When(x => !string.IsNullOrWhiteSpace(x.UpdateData.Text));

            RuleFor(x => x.UpdateData.Pronunciation)
                .MaximumLength(200)
                .WithName("Pronunciation")
                .When(x => !string.IsNullOrWhiteSpace(x.UpdateData.Pronunciation));

            RuleFor(x => x.UpdateData.Definition)
                .MaximumLength(500) 
                .WithName("Definition")
                .When(x => x.UpdateData.Definition != null);

            RuleFor(x => x.UpdateData.ImgURL)
                .MaximumLength(1000)
                .WithName("ImgURL")
                .When(x => !string.IsNullOrWhiteSpace(x.UpdateData.ImgURL));
        }
    }
}