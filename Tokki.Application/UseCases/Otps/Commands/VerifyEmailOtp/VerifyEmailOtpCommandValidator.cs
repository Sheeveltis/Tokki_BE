using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp
{
    public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
    {
        public VerifyEmailOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255)
                .WithName("Email");

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Length(6) 
                .WithName("Mã OTP");
        }
    }
}