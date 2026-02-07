using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailAutoTemplateCommandValidator : AbstractValidator<CreateEmailAutoTemplateCommand>
    {
        public CreateEmailAutoTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên mẫu email");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithName("Loại template");

            RuleFor(x => x.Value)
                .GreaterThan(0)
                .WithMessage("'{PropertyName}' phải lớn hơn 0.")
                .WithName("Mốc thời gian (ngày)");

            RuleFor(x => x.TargetGroup)
                .IsInEnum()
                .WithName("Nhóm người nhận");

            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề email");

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithName("Nội dung email");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithName("Mô tả");

            // (Khuyến nghị nghiệp vụ) Type=2 chỉ nên gửi cho VIP/All
            RuleFor(x => x)
                .Must(x => x.Type != EmailTemplateType.VipExpiringReminder
                           || x.TargetGroup == UserTargetGroup.All
                           || x.TargetGroup == UserTargetGroup.VipUsers)
                .WithMessage("Template 'VIP sắp hết hạn' chỉ hợp lệ với nhóm All hoặc VipUsers.");
        }
    }
}
