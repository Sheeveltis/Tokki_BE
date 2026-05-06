using FluentValidation;

namespace Tokki.Application.UseCases.Alphabet.Commands.CreateAlphabet
{
    public class CreateAlphabetCommandValidator : AbstractValidator<CreateAlphabetCommand>
    {
        public CreateAlphabetCommandValidator()
        {
            RuleFor(x => x.Letter)
                .NotEmpty().WithMessage("Ký tự không được để trống.")
                .MaximumLength(10).WithMessage("Ký tự không được quá 10 ký tự.");

            RuleFor(x => x.Meaning)
                .MaximumLength(100).WithMessage("Nghĩa không được quá 100 ký tự.");

            RuleFor(x => x.Pronunciation)
                .MaximumLength(50).WithMessage("Phát âm không được quá 50 ký tự.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại ký tự không hợp lệ.");
        }
    }
}
