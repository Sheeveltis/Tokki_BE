using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex
{
    public class UpdateTopicOrderIndexCommandValidator : AbstractValidator<UpdateTopicOrderIndexCommand>
    {
        public UpdateTopicOrderIndexCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithMessage("TopicId không được để trống.");

            RuleFor(x => x.OrderIndex)
                .GreaterThan(0)
                .WithMessage("OrderIndex phải >= 1.");
        }
    }
}