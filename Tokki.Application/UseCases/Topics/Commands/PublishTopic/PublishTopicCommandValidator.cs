using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.PublishTopic
{
    public class PublishTopicCommandValidator : AbstractValidator<PublishTopicCommand>
    {
        public PublishTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithName("TopicId");
        }
    }
}
