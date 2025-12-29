using FluentValidation;
using Tokki.Application.UseCases.Vocabulary.DTOs;

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

            // Báo cụ thể sentence nào bị trùng trong input
            RuleFor(x => x.Examples)
                .Custom((examples, context) =>
                {
                    if (examples == null || examples.Count == 0) return;

                    // Normalize + giữ lại câu gốc để hiển thị
                    var normalized = examples
                        .Select(e => e?.Sentence?.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    if (normalized.Count == 0) return;

                    var duplicates = normalized
                        .GroupBy(s => s!, StringComparer.OrdinalIgnoreCase)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicates.Any())
                    {
                        context.AddFailure(
                            "Examples",
                            $"Danh sách câu ví dụ bị trùng: {string.Join(" | ", duplicates)}"
                        );
                    }
                });

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
