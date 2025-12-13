using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            // 1. Validate Email
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithName("Email");

            // 2. Validate Mật khẩu cũ
            RuleFor(x => x.OldPassword)
                .NotEmpty()
                .WithName("Mật khẩu cũ");

            // 3. Validate Mật khẩu mới
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(6)
                .MaximumLength(100)
                .NotEqual(x => x.OldPassword) 
                .WithName("Mật khẩu mới");

            // 4. Validate Xác nhận mật khẩu
            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword) 
                .WithName("Mật khẩu nhập lại");
        }
    }
}