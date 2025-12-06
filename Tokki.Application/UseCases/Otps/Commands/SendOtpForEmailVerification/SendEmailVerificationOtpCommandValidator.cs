using FluentValidation;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;

namespace Tokki.Application.UseCases.Accounts.Commands.SendOtpForEmailVerification
{
    public class SendEmailVerificationOtpCommandValidator : AbstractValidator<SendEmailVerificationOtpCommand>
    {
        public SendEmailVerificationOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Vui lòng nhập email.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");
        }
    }
}