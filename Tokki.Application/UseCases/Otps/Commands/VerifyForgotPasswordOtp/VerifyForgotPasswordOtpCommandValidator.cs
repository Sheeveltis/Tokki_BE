using FluentValidation;
using Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp;

namespace Tokki.Application.UseCases.Otps.Validators
{
    public class VerifyForgotPasswordOtpCommandValidator : AbstractValidator<VerifyForgotPasswordOtpCommand>
    {
        public VerifyForgotPasswordOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Length(6).WithMessage("Mã OTP phải có đúng 6 ký tự.")
                .Matches("^[0-9]+$").WithMessage("Mã OTP chỉ được chứa số.");
        }
    }
}