using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống.")
                .MaximumLength(255).WithMessage("Họ tên tối đa 255 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.")
                .Matches(@"^\d+$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Số điện thoại chỉ được chứa số.");

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.AddHours(7)).WithMessage("Ngày sinh phải nhỏ hơn ngày hiện tại.");
        }
    }
}