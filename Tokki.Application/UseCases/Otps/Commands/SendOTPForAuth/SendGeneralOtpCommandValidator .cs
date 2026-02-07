using FluentValidation;

namespace Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp
{
    public class SendGeneralOtpCommandValidator : AbstractValidator<SendGeneralOtpCommand>
    {
        public SendGeneralOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()     
                .MaximumLength(255)  
                .WithName("Email");
        }
    }
}