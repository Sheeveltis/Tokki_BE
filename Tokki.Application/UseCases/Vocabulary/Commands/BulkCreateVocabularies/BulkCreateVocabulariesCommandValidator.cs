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

<<<<<<< HEAD
               
=======
                vocab.RuleFor(v => v.Pronunciation)
                    .MaximumLength(255)
                    .When(v => !string.IsNullOrEmpty(v.Pronunciation))
                    .WithName("Pronunciation");

                vocab.RuleFor(v => v.Examples)
                    .Must(examples => examples == null || examples.Count <= 10)
                    .WithMessage("Mỗi vocabulary không thể có quá 10 câu ví dụ.");

                vocab.RuleForEach(v => v.Examples)
                    .ChildRules(example =>
                    {
                        example.RuleFor(e => e.Sentence)
                            .NotEmpty()
                            .WithMessage("Câu ví dụ không được để trống.");

                        example.RuleFor(e => e.Translation)
                            .MaximumLength(1000)
                            .When(e => !string.IsNullOrEmpty(e.Translation))
                            .WithMessage("Bản dịch không được vượt quá 1000 ký tự.");
                    })
                    .When(v => v.Examples != null);
>>>>>>> 519bc38f4c1de86d626062dd3e0674f2cf6e5803
            });
        }
    }
}