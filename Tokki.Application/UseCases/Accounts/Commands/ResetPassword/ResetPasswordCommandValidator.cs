using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.NewPassword).MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự.");
            RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage("Mật khẩu nhập lại không khớp.");
        }
    }
}
