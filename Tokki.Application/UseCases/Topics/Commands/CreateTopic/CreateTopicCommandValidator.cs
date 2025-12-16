using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopic
{
    public class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
    {
        public CreateTopicCommandValidator()
        {
            RuleFor(x => x.TopicName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên chủ đề");

            RuleFor(x => x.Description)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithName("Mô tả");
            RuleFor(x => x.Level)
                .IsInEnum().WithMessage("Cấp độ không hợp lệ.")
                .WithName("Cấp độ");

        }
    }
}