using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty() // Tự động: "'{PropertyName}' không được để trống."
                .EmailAddress(); // Tự động: "'{PropertyName}' không đúng định dạng email."

            RuleFor(x => x.Password)
                .NotEmpty(); // Tự động: "'{PropertyName}' không được để trống."
        }
    }
}