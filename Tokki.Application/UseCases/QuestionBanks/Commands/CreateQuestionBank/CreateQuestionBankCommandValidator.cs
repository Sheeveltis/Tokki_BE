using FluentValidation;
using Tokki.Domain.Enums;

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

            // Nếu là Writing (3), không được có Options
            RuleFor(x => x.Options)
                .Must(opts => opts == null || !opts.Any())
                .When(x => x.Skill == QuestionSkill.Writing)
                .WithMessage("Câu hỏi tự luận (Writing) không được có đáp án trắc nghiệm");

            // Nếu KHÔNG phải Writing, phải có 2-4 Options
            RuleFor(x => x.Options)
                .NotEmpty()
                .When(x => x.Skill != QuestionSkill.Writing)
                .WithMessage("Câu hỏi trắc nghiệm phải có ít nhất 2 đáp án");

            RuleFor(x => x.Options)
                .Must(opts => opts.Count >= 2 && opts.Count <= 4)
                .When(x => x.Skill != QuestionSkill.Writing && x.Options != null)
                .WithMessage("Số đáp án phải từ 2 đến 4");

            // Validate từng Option (chỉ khi KHÔNG phải Writing)
            When(x => x.Skill != QuestionSkill.Writing, () =>
            {
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
            });
        }
    }
}