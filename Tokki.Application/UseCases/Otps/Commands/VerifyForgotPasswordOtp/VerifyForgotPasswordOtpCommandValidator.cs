using FluentValidation;

namespace Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp
{
    public class VerifyForgotPasswordOtpCommandValidator : AbstractValidator<VerifyForgotPasswordOtpCommand>
    {
        public VerifyForgotPasswordOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress() 
                .MaximumLength(255)
                .WithName("Email");

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Length(6) 
                .Matches(@"^\d+$").WithMessage("'{PropertyName}' chỉ được chứa các ký tự số.")
                .WithName("Mã OTP");
        }
    }
}