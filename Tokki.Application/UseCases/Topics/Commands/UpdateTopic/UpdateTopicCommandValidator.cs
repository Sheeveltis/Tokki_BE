using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopic
{
    public class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
    {
        public UpdateTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithMessage("TopicId không được để trống.");

            When(x => x.TopicName != null, () =>
            {
                RuleFor(x => x.TopicName!)
                    .MaximumLength(100)
                    .WithMessage("TopicName không được vượt quá 100 ký tự.");
                // Không bắt NotEmpty ở đây vì bạn muốn "truyền rỗng thì bỏ qua"
            });

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description!)
                    .MaximumLength(255)
                    .WithMessage("Description không được vượt quá 255 ký tự.");
            });

            When(x => x.ImgUrl != null, () =>
            {
                RuleFor(x => x.ImgUrl!)
                    .MaximumLength(500)
                    .WithMessage("ImgUrl không được vượt quá 500 ký tự.");
            });

            When(x => x.Level.HasValue, () =>
            {
                RuleFor(x => x.Level!.Value)
                    .IsInEnum()
                    .WithMessage("Level không hợp lệ.");
            });

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .IsInEnum()
                    .WithMessage("Status không hợp lệ.");
            });
        }
    }
}
