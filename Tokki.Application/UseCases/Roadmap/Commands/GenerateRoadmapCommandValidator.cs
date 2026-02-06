using FluentValidation;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommandValidator : AbstractValidator<GenerateRoadmapCommand>
    {
        public GenerateRoadmapCommandValidator()
        {
            RuleFor(x => x.TargetAim)
                .NotEmpty().WithMessage("Mục tiêu học tập không được để trống.");

            RuleFor(x => x.DurationDays)
                .Must(d => d == 30 || d == 60 || d == 90)
                .WithMessage("Thời gian lộ trình chỉ chấp nhận 30, 60 hoặc 90 ngày.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Không xác định được người dùng.");
        }
    }
}