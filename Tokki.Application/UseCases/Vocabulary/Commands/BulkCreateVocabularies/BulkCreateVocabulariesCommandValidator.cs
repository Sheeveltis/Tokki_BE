using FluentValidation;

namespace Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies
{
    public class BulkCreateVocabulariesCommandValidator : AbstractValidator<BulkCreateVocabulariesCommand>
    {
        public BulkCreateVocabulariesCommandValidator()
        {
            RuleFor(x => x.Vocabularies)
                .NotEmpty() 
                .WithName("Danh sách vocabulary") 
                .Must(x => x.Count <= 100)
                .WithMessage("Không thể tạo quá 100 vocabulary trong một lần.");

            RuleForEach(x => x.Vocabularies).ChildRules(vocab =>
            {
                vocab.RuleFor(v => v.Text)
                    .NotEmpty()
                    .MaximumLength(100) 
                    .WithName("Text");

                vocab.RuleFor(v => v.Definition)
                    .NotEmpty()
                    .MaximumLength(500)
                    .WithName("Definition");

                vocab.RuleFor(v => v.ExampleSentence)
                    .MaximumLength(500)
                    .WithName("ExampleSentence")
                    .When(v => !string.IsNullOrEmpty(v.ExampleSentence));

                vocab.RuleFor(v => v.TopicIds)
                    .Must(x => x == null || x.Count <= 20)
                    .WithMessage("Một vocabulary không thể thuộc quá 20 topics.") 
                    .WithName("Danh sách chủ đề");
            });
        }
    }
}