using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic
{
    public class AddVocabulariesToTopicCommandValidator
        : AbstractValidator<AddVocabulariesToTopicCommand>
    {
        public AddVocabulariesToTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithName("TopicId");

            RuleFor(x => x.VocabularyIds)
                .NotEmpty()
                .WithName("Danh sách từ vựng");

            RuleForEach(x => x.VocabularyIds)
                .NotEmpty()
                .WithName("VocabularyId");
        }
    }
}
