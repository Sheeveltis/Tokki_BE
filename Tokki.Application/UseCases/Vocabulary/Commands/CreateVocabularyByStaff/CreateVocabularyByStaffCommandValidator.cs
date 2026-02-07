using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff
{
    public class CreateVocabularyByStaffCommandValidator
        : AbstractValidator<CreateVocabularyByStaffCommand>
    {
        public CreateVocabularyByStaffCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Definition)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Pronunciation)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Pronunciation));

            RuleFor(x => x.Examples)
                .Must(x => x == null || x.Count <= 10)
                .WithMessage("Không thể thêm quá 10 câu ví dụ.");

            RuleForEach(x => x.Examples)
                .ChildRules(example =>
                {
                    example.RuleFor(e => e.Sentence)
                        .NotEmpty();

                    example.RuleFor(e => e.Translation)
                        .MaximumLength(1000)
                        .When(e => !string.IsNullOrEmpty(e.Translation));
                })
                .When(x => x.Examples != null);
        }
    }
}
