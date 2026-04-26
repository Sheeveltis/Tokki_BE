using FluentValidation;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.ChangeSortOrder
{
    public class ChangePronunciationExampleSortOrderCommandValidator : AbstractValidator<ChangePronunciationExampleSortOrderCommand>
    {
        public ChangePronunciationExampleSortOrderCommandValidator()
        {
            RuleFor(x => x.ExampleId)
                .NotEmpty().WithMessage("ID ví dụ là bắt buộc.");

            RuleFor(x => x.NewSortOrder)
                .GreaterThan(0).WithMessage("Thứ tự mới phải lớn hơn 0.");
        }
    }
}
