using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount
{
    public class CreateStaffAccountCommandValidator : AbstractValidator<CreateStaffAccountCommand>
    {
        public CreateStaffAccountCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(255);

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống")
                .MaximumLength(100);

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d+$").WithMessage("Số điện thoại chỉ được chứa số")
                .MaximumLength(15)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
               .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("Ngày sinh phải nhỏ hơn ngày hiện tại");
        }
    }
}
