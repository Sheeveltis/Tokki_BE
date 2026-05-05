using FluentValidation;

namespace Tokki.Application.UseCases.Alphabet.Commands.UpdateAlphabet
{
    public class UpdateAlphabetCommandValidator : AbstractValidator<UpdateAlphabetCommand>
    {
        public UpdateAlphabetCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id không được để trống.");

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
