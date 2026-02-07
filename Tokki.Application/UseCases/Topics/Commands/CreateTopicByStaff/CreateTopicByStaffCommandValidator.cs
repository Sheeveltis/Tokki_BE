using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff
{
    public class CreateTopicByStaffCommandValidator
        : AbstractValidator<CreateTopicByStaffCommand>
    {
        public CreateTopicByStaffCommandValidator()
        {
            RuleFor(x => x.TopicName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên chủ đề");

            RuleFor(x => x.Description)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Level)
                .IsInEnum()
                .WithMessage("Cấp độ không hợp lệ.");
        }
    }
}
