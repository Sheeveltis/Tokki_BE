using FluentValidation;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank
{
    public class CreateQuestionBankCommandValidator : AbstractValidator<CreateQuestionBankCommand>
    {
        public CreateQuestionBankCommandValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithName("Nội dung câu hỏi");

            RuleFor(x => x.Skill)
                .IsInEnum()
                .WithName("Kỹ năng");

            RuleFor(x => x.DifficultyLevel)
                .IsInEnum()
                .WithName("Mức độ");

            RuleFor(x => x.Options)
                .NotEmpty().WithMessage("Phải có ít nhất 2 đáp án")
                .Must(opts => opts.Count >= 2 && opts.Count <= 4)
                .WithMessage("Số đáp án phải từ 2 đến 4");

            RuleForEach(x => x.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.KeyOption)
                    .NotEmpty()
                    .Must(k => new[] { "1", "2", "3", "4" }.Contains(k))
                    .WithMessage("KeyOption phải là '1', '2', '3' hoặc '4'");

                option.RuleFor(o => o.Content)
                    .NotEmpty()
                    .When(o => string.IsNullOrEmpty(o.ImageUrl))
                    .WithMessage("Đáp án phải có nội dung text hoặc ảnh");
            });
        }
    }
}