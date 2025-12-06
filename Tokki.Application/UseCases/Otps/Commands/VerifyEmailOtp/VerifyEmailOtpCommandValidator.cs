using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp
{
    public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
    {
        public VerifyEmailOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Vui lòng nhập email.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");

            RuleFor(x => x.OtpCode)
                .NotEmpty().WithMessage("Vui lòng nhập mã OTP.")
                .Length(6).WithMessage("Mã OTP phải có 6 ký tự.");
        }
    }
}