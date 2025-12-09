using FluentValidation;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;

namespace Tokki.Application.UseCases.Otps.Validators
{
    public class SendForgotPasswordOtpCommandValidator : AbstractValidator<SendForgotPasswordOtpCommand>
    {
        public SendForgotPasswordOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);
        }
    }
}