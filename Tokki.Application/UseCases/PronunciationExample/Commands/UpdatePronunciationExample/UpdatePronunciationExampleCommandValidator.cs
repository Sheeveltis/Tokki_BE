using FluentValidation;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample
{
    public class UpdatePronunciationExampleCommandValidator : AbstractValidator<UpdatePronunciationExampleCommand>
    {
        public UpdatePronunciationExampleCommandValidator()
        {
            RuleFor(x => x.ExampleId)
                .NotEmpty().WithMessage("Mã ví dụ là bắt buộc.");

            RuleFor(x => x.TargetScript)
                .NotEmpty().WithMessage("Nội dung mục tiêu (Target Script) là bắt buộc.")
                .MaximumLength(500).WithMessage("Nội dung mục tiêu không vượt quá 500 ký tự.");

            RuleFor(x => x.RawScript)
                .NotEmpty().WithMessage("Nội dung (Raw Script) là bắt buộc.")
                .MaximumLength(500).WithMessage("Nội dung (Raw Script) không vượt quá 500 ký tự.");

            RuleFor(x => x.PhoneticScript)
                .NotEmpty().WithMessage("Phiên âm (Phonetic Script) là bắt buộc.")
                .MaximumLength(500).WithMessage("Phiên âm không vượt quá 500 ký tự.");

            RuleFor(x => x.Meaning)
                .MaximumLength(500).WithMessage("Nghĩa không vượt quá 500 ký tự.");

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Thứ tự sắp xếp phải >= 0.");
        }
    }
}
