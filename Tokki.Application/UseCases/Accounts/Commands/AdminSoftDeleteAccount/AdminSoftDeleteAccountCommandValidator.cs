using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount
{
    public class AdminSoftDeleteAccountCommandValidator : AbstractValidator<AdminSoftDeleteAccountCommand>
    {
        public AdminSoftDeleteAccountCommandValidator()
        {
            RuleFor(x => x.TargetUserId)
               .NotEmpty()
               .WithName("userId của người cần xóa");
        }
    }
}
