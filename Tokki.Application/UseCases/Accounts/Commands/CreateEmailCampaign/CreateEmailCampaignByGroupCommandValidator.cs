using FluentValidation;
using System;
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;

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
        }
    }
}