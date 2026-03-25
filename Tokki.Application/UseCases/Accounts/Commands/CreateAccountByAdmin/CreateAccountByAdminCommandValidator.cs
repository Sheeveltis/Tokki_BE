using System;
using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount
{
    public class CreateAccountByAdminCommandValidator
        : AbstractValidator<CreateAccountByAdminCommand>
    {
        public CreateAccountByAdminCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc.")
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .MaximumLength(255).WithMessage("Email không được vượt quá 255 ký tự.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên là bắt buộc.")
                .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^0(86|96|97|98|32|33|34|35|36|37|38|39|89|90|93|70|76|77|78|79|88|91|94|81|82|83|84|85|92|52|56|58|99|59|87|55)\d{7}$")
                .WithMessage("Số điện thoại không hợp lệ. Vui lòng nhập đúng số di động Việt Nam (10 chữ số, bắt đầu bằng 0).")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("Ngày sinh phải là ngày trong quá khứ.");

            RuleFor(x => x.Role)
    .Must(BeValidRole).WithMessage("Vai trò chỉ có thể là Staff hoặc User.");
        }

        private bool BeValidRole(AccountRole role)
        {
            return role == AccountRole.Staff ||
                   role == AccountRole.User;
        }
    }
}