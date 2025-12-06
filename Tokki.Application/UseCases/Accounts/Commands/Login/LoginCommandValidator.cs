using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Vui lòng nhập email.")
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu.");
            // Lưu ý: Đăng nhập thì không nên check độ dài mật khẩu (như MinLength) 
            // để tránh lộ thông tin bảo mật cho Hacker. Chỉ cần check NotEmpty là đủ.
        }
    }
}