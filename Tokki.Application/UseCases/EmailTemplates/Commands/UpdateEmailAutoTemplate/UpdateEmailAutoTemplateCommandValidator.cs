using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate
{
    public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailAutoTemplateCommand>
    {
        public UpdateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithName("ID mẫu email");

            RuleFor(x => x.TemplateName)
                .MaximumLength(100)
                .WithName("Tên mẫu email")
                .When(x => !string.IsNullOrWhiteSpace(x.TemplateName));

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithName("Loại template")
                .When(x => x.Type.HasValue);

            RuleFor(x => x.Value)
                .GreaterThan(0)
                .WithMessage("'{PropertyName}' phải lớn hơn 0.")
                .WithName("Mốc thời gian (ngày)")
                .When(x => x.Value.HasValue);

            RuleFor(x => x.TargetGroup)
                .IsInEnum()
                .WithName("Nhóm người nhận")
                .When(x => x.TargetGroup.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithName("Trạng thái")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Subject)
                .MaximumLength(255)
                .WithName("Tiêu đề email")
                .When(x => !string.IsNullOrWhiteSpace(x.Subject));

           
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithName("Mô tả")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            // Rule nghiệp vụ: chỉ chạy khi client có truyền cả Type và TargetGroup
            RuleFor(x => x)
                .Must(x =>
                    !x.Type.HasValue || !x.TargetGroup.HasValue ||
                    x.Type.Value != EmailTemplateType.VipExpiringReminder ||
                    x.TargetGroup.Value == UserTargetGroup.All ||
                    x.TargetGroup.Value == UserTargetGroup.VipUsers
                )
                .WithMessage("Template 'VIP sắp hết hạn' chỉ hợp lệ với nhóm All hoặc VipUsers.");
        }
    }
}
