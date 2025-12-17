using System;
using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount
{
    public class CreateStaffAndAdminAccountCommandValidator
        : AbstractValidator<CreateStaffAndAdminAccountCommand>
    {
        public CreateStaffAndAdminAccountCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d+$")
                .MaximumLength(15)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.Now));
            RuleFor(x => x.Role)
              .NotEmpty().WithMessage("Vai trò là bắt buộc.")
              .Must(BeValidRole).WithMessage("Vai trò chỉ có thể là Staff hoặc Admin.");
        
        }
        private bool BeValidRole(AccountRole role)
        {
            return role == AccountRole.Staff || role == AccountRole.Admin;
        }
    }
}
