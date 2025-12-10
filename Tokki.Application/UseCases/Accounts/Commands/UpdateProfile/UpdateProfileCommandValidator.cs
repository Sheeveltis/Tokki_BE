using FluentValidation;
using System;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            // 1. Validate Họ và tên
            RuleFor(x => x.FullName)
                .MaximumLength(255)
                .WithName("Họ và tên");

            // 2. Validate Số điện thoại
            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .Matches(@"^\d+$").WithMessage("Số điện thoại chỉ được chứa các ký tự số.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithName("Số điện thoại");

            RuleFor(x => x.DateOfBirth)
                // Loại bỏ .NotEmpty()
                .LessThan(DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("Ngày sinh không hợp lệ (phải nhỏ hơn ngày hiện tại).")
                .When(x => x.DateOfBirth.HasValue) // Chỉ validate khi có giá trị gửi lên
                .WithName("Ngày sinh");
        }
    }
}