using FluentValidation;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.Reorder
{
    public class ChangePronunciationRuleSortOrderCommandValidator : AbstractValidator<ChangePronunciationRuleSortOrderCommand>
    {
        public ChangePronunciationRuleSortOrderCommandValidator()
        {
            RuleFor(x => x.PronunciationRuleId)
                .NotEmpty().WithMessage("ID quy tắc là bắt buộc.");

            RuleFor(x => x.NewSortOrder)
                .GreaterThan(0).WithMessage("Thứ tự mới phải lớn hơn 0.");
        }
    }
}
