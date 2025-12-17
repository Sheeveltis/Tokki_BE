using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary
{
    public class CreateVocabularyCommandValidator : AbstractValidator<CreateVocabularyCommand>
    {
        public CreateVocabularyCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty()
                .WithMessage("Text không được để trống.")
                .MaximumLength(100)
                .WithMessage("Text không được vượt quá 100 ký tự.");

            RuleFor(x => x.Definition)
                .NotEmpty()
                .WithMessage("Definition không được để trống.")
                .MaximumLength(500)
                .WithMessage("Definition không được vượt quá 500 ký tự.");

            RuleFor(x => x.Pronunciation)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Pronunciation))
                .WithMessage("Pronunciation không được vượt quá 255 ký tự.");

            RuleFor(x => x.Examples)
                .Must(examples => examples == null || examples.Count <= 10)
                .WithMessage("Không thể thêm quá 10 câu ví dụ.");

            RuleForEach(x => x.Examples)
                .ChildRules(example =>
                {
                    example.RuleFor(e => e.Sentence)
                        .NotEmpty()
                        .WithMessage("Câu ví dụ không được để trống.");

                    example.RuleFor(e => e.Translation)
                        .MaximumLength(1000)
                        .When(e => !string.IsNullOrEmpty(e.Translation))
                        .WithMessage("Bản dịch không được vượt quá 1000 ký tự.");
                })
                .When(x => x.Examples != null);
        }
    }
}