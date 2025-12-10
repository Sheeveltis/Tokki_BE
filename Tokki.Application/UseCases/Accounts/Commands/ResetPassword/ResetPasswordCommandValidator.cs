using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
           

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(6)
                .MaximumLength(100)
                .WithName("Mật khẩu mới");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword)
                .WithName("Mật khẩu nhập lại");
        }
    }
}