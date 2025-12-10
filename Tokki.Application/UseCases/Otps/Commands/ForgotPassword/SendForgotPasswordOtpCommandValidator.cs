using FluentValidation;

namespace Tokki.Application.UseCases.Otps.Commands.ForgotPassword
{
    public class SendForgotPasswordOtpCommandValidator : AbstractValidator<SendForgotPasswordOtpCommand>
    {
        public SendForgotPasswordOtpCommandValidator()
        {
            // Validate Email
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255)
                .WithName("Email");
        }
    }
}