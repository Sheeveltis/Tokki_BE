using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class EmailTemplateTestData
    {
        public static CreateEmailAutoTemplateCommand GetValidCreateEmailAutoTemplateCommand()
        {
            return new CreateEmailAutoTemplateCommand
            {
                TemplateName = "VIP Expiring Reminder - 7 Days",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Subject = "[Tokki] VIP is about to expire",
                Body = "<p>Your VIP will expire in 7 days.</p>",
                Description = "Remind VIP is about to expire"
            };
        }

        public static EmailTemplate GetExistingTemplateByName(string templateName, EmailTemplateStatus status = EmailTemplateStatus.Draft)
        {
            return new EmailTemplate
            {
                TemplateId = "tpl-old-01",
                TemplateName = templateName,
                Status = status,
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Subject = "Old subject",
                Body = "Old body",
                Description = "Old desc",
                CreateAt = DateTime.UtcNow.AddHours(7).AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };
        }

        public static EmailTemplate GetExistingTemplateByLogic(
            EmailTemplateType type,
            int value,
            UserTargetGroup targetGroup,
            EmailTemplateStatus status = EmailTemplateStatus.Draft)
        {
            return new EmailTemplate
            {
                TemplateId = "tpl-old-02",
                TemplateName = "Some other name",
                Status = status,
                Type = type,
                Value = value,
                TargetGroup = targetGroup,
                Subject = "Old subject",
                Body = "Old body",
                Description = "Old desc",
                CreateAt = DateTime.UtcNow.AddHours(7).AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };
        }
    }
}
