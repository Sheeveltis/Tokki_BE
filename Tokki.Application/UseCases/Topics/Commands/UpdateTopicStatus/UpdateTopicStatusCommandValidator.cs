using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus
{
    public class UpdateTopicStatusCommandValidator : AbstractValidator<UpdateTopicStatusCommand>
    {
        public UpdateTopicStatusCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithMessage("TopicId không được để trống.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Status không hợp lệ.");
        }
    }
}
