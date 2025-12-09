using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
                .WithName("Email");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithName("Mật khẩu");
        }
    }
}