using FluentValidation;
using System;
using Tokki.Application.UseCases.Accounts.Commands.Register;
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
                .Matches(@"^0(86|96|97|98|32|33|34|35|36|37|38|39|89|90|93|70|76|77|78|79|88|91|94|81|82|83|84|85|92|52|56|58|99|59|87|55)\d{7}$")
                .WithMessage("Số điện thoại không hợp lệ. Vui lòng nhập đúng số di động Việt Nam (10 chữ số, bắt đầu bằng 0).")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithName("Số điện thoại");

           
            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .LessThan(DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("Ngày sinh không hợp lệ (phải nhỏ hơn ngày hiện tại).")
                .WithName("Ngày sinh");
        }
    }
}