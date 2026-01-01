using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign
{
    public class UpdateEmailCampaignCommandValidator : AbstractValidator<UpdateEmailCampaignCommand>
    {
        public UpdateEmailCampaignCommandValidator()
        {
            RuleFor(x => x.JobId)
                .NotEmpty()
                .WithName("JobId");

            // Subject: nếu truyền thì validate
            When(x => x.Subject != null, () =>
            {
                RuleFor(x => x.Subject)
                    .NotEmpty()
                    .MaximumLength(255)
                    .WithName("Tiêu đề email");
            });

            // Body: nếu truyền thì validate
            When(x => x.Body != null, () =>
            {
                RuleFor(x => x.Body)
                    .NotEmpty()
                    .WithName("Nội dung email");
            });

            // TargetGroup: nếu truyền thì phải hợp lệ enum
            When(x => x.TargetGroup.HasValue, () =>
            {
                RuleFor(x => x.TargetGroup!.Value)
                    .IsInEnum()
                    .WithName("Nhóm khách hàng mục tiêu");
            });

            // ScheduledTime: nếu truyền thì phải lớn hơn hiện tại (UTC+7)
            RuleFor(x => x.ScheduledTime)
                .Must(t => !t.HasValue || t.Value > DateTime.UtcNow.AddHours(7))
                .WithMessage("Thời gian lên lịch gửi phải lớn hơn thời gian hiện tại.")
                .WithName("Thời gian gửi");

            // SpecificEmails: nếu truyền list thì validate format
            When(x => x.SpecificEmails != null, () =>
            {
                RuleFor(x => x.SpecificEmails!)
                    .Must(list => list.All(IsValidEmail))
                    .WithMessage("Danh sách email chứa địa chỉ không hợp lệ");
            });

            // Nếu user set Status thì chỉ cho Deleted
            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .Equal(EmailJobStatus.Deleted)
                    .WithMessage("Chỉ cho phép cập nhật Status sang Deleted (xóa mềm).");
            });

            // Bắt buộc có người nhận sau khi update:
            // - Nếu TargetGroup=None thì phải có SpecificEmails (nếu client truyền TargetGroup hoặc SpecificEmails)
            RuleFor(x => x)
                .Must(x =>
                    !x.TargetGroup.HasValue ||
                    x.TargetGroup.Value != Domain.Enums.UserTargetGroup.None ||
                    (x.SpecificEmails != null && x.SpecificEmails.Any()))
                .WithMessage("Nếu TargetGroup = None thì phải có SpecificEmails.")
                .WithName("Người nhận");
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email.Trim());
                return addr.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
