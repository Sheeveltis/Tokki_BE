using FluentValidation;
using System;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.Application.UseCases.Accounts.Commands.Register
{
    public class RegisterUserAccountCommandValidator : AbstractValidator<RegisterUserAccountCommand>
    {
        public RegisterUserAccountCommandValidator()
        {
            RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .MaximumLength(255)
    .WithName("Email");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(6)
                .MaximumLength(100)
                .WithName("Mật khẩu");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Họ và tên");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(15)
                .Matches(@"^\d+$")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithName("Số điện thoại");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .LessThan(DateOnly.FromDateTime(DateTime.Today))
                .WithName("Ngày sinh");

        }
    }
}