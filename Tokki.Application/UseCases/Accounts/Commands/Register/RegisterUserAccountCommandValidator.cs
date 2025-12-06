using FluentValidation;
using System;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.Application.UseCases.Accounts.Commands.Register
{
    public class RegisterUserAccountCommandValidator : AbstractValidator<RegisterUserAccountCommand>
    {
        public RegisterUserAccountCommandValidator()
        {
            // 1. Validate Email
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
                .MaximumLength(255)
                .WithName("Email");

            // 2. Validate Password (Mật khẩu)
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.")
                .MaximumLength(100)
                // Tùy chọn: Bắt buộc có chữ hoa, chữ thường, số (nếu cần bảo mật cao)
                // .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ in hoa.")
                // .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 số.")
                .WithName("Mật khẩu");

            // 3. Validate FullName (Họ tên)
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Họ và tên");

            // 4. Validate PhoneNumber (Số điện thoại) - Chỉ check khi có dữ liệu
            RuleFor(x => x.PhoneNumber)
                .MaximumLength(15)
                .Matches(@"^\d+$").WithMessage("Số điện thoại chỉ được chứa ký tự số.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber)) // Chỉ validate khi user có nhập
                .WithName("Số điện thoại");

            // 5. Validate DateOfBirth (Ngày sinh)
            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Vui lòng nhập Ngày sinh.")
                .LessThan(DateOnly.FromDateTime(DateTime.Today)) // 👈 Sửa thành DateTime.Today để so sánh chuẩn
                .WithMessage("Ngày sinh không thể là ngày hiện tại hoặc tương lai.")
                .WithName("Ngày sinh");
        }
    }
}