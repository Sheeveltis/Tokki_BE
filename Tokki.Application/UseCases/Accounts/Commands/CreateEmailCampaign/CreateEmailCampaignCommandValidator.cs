using FluentValidation;
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;

namespace Tokki.Application.UseCases.Email.Validators
{
    public class CreateEmailCampaignCommandValidator : AbstractValidator<CreateEmailCampaignCommand>
    {
        public CreateEmailCampaignCommandValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(200).WithMessage("Tiêu đề email không được vượt quá 200 ký tự.");

            RuleFor(x => x.Body)
                .NotEmpty()
                .MaximumLength(5000).WithMessage("Nội dung email không được vượt quá 5000 ký tự.");

            RuleFor(x => x.TargetGroup)
                .IsInEnum().WithMessage("Nhóm đối tượng không hợp lệ.");

            RuleFor(x => x.ScheduledTime)
                .Must(BeAValidFutureDate)
                .When(x => x.ScheduledTime.HasValue)
                .WithMessage("Thời gian lên lịch phải là thời điểm trong tương lai.");
        }

        private bool BeAValidFutureDate(DateTime? date)
        {
            if (!date.HasValue) return true;
            return date.Value > DateTime.UtcNow.AddHours(7);
        }
    }
}