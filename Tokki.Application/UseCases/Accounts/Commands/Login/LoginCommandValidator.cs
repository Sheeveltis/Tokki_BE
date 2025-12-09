using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithName("Email");

            //RuleFor(x => x.Password)
            //    .NotEmpty().WithMessage("Vui lòng nhập mật khẩu.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithName("Mật khẩu");

            // Lưu ý: Đăng nhập thì không nên check độ dài mật khẩu (như MinLength) 
            // để tránh lộ thông tin bảo mật cho Hacker. Chỉ cần check NotEmpty là đủ.
        }
    }
}