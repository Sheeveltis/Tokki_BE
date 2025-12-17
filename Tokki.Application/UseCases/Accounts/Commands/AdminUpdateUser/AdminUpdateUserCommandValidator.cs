
using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommandValidator : AbstractValidator<AdminUpdateUserCommand>
    {
        public AdminUpdateUserCommandValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Role).IsInEnum().WithMessage("Role không hợp lệ.");
            RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái không hợp lệ.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d+$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Số điện thoại chỉ được chứa số.");
        }
    }
}
