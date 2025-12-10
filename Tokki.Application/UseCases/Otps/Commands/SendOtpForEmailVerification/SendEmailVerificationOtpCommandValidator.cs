using FluentValidation;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;

namespace Tokki.Application.UseCases.Accounts.Commands.SendOtpForEmailVerification
{
    public class SendEmailVerificationOtpCommandValidator : AbstractValidator<SendEmailVerificationOtpCommand>
    {
        public SendEmailVerificationOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()         
                .EmailAddress()    
                .MaximumLength(255)  
                .WithName("Email");
        }
    }
}