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
                .Matches(@"^\d+$").WithMessage("Số điện thoại chỉ được chứa các chữ số.")
                .MaximumLength(15).WithMessage("Số điện thoại không được vượt quá 15 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("Ngày sinh phải là ngày trong quá khứ.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Vai trò là bắt buộc.")
                .Must(BeValidRole).WithMessage("Vai trò chỉ có thể là Staff, Admin hoặc User.");
        }

        private bool BeValidRole(AccountRole role)
        {
            return role == AccountRole.Staff ||
                   role == AccountRole.User;
        }
    }
}