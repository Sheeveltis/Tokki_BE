using System;
using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount
{
    public class CreateStaffAccountCommandValidator
        : AbstractValidator<CreateStaffAccountCommand>
    {
        public CreateStaffAccountCommandValidator()
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
        }
    }
}
