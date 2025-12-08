using FluentValidation;

namespace Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp
{
    public class SendGeneralOtpCommandValidator : AbstractValidator<SendGeneralOtpCommand>
    {
        public SendGeneralOtpCommandValidator()
        {
            // CODE Validation như trong hình của bạn

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
                .WithName("Email");
        }
    }
}
