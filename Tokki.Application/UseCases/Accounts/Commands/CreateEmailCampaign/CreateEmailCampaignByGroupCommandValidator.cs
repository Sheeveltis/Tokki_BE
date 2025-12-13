using FluentValidation;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign
{
    public class CreateEmailCampaignByGroupCommandValidator : AbstractValidator<CreateEmailCampaignByGroupCommand>
    {
        public CreateEmailCampaignByGroupCommandValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề email");

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithName("Nội dung email");

            RuleFor(x => x.TargetGroup)
                .IsInEnum()
                .WithName("Nhóm khách hàng mục tiêu");

            RuleFor(x => x.ScheduledTime)
                .Must(time => time.Value > DateTime.UtcNow.AddHours(7))
                .When(x => x.ScheduledTime.HasValue)
                .WithMessage("Thời gian lên lịch gửi phải lớn hơn thời gian hiện tại.")
                .WithName("Thời gian gửi");

            // ✅ Validate danh sách email
            RuleFor(x => x.SpecificEmails)
                .Must(emails => emails == null || emails.All(e => IsValidEmail(e)))
                .When(x => x.SpecificEmails != null && x.SpecificEmails.Any())
                .WithMessage("Danh sách email chứa địa chỉ không hợp lệ");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}