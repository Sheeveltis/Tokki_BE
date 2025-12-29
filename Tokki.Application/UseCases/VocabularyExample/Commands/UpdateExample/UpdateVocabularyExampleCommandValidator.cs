using FluentValidation;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample
{
    public class UpdateVocabularyExampleCommandValidator : AbstractValidator<UpdateVocabularyExampleCommand>
    {
        public UpdateVocabularyExampleCommandValidator()
        {
            RuleFor(x => x.ExampleId)
                .NotEmpty()
                .WithName("ExampleId");

            RuleFor(x => x.UpdateData)
                .NotNull()
                .WithName("UpdateData");

            // Chỉ validate Sentence khi client có truyền Sentence (update partial)
            When(x => x.UpdateData != null && x.UpdateData.Sentence != null, () =>
            {
                RuleFor(x => x.UpdateData!.Sentence)
                    .NotEmpty()
                    .WithName("Sentence");
            });

            // Chỉ validate Translation khi client có truyền Translation
            When(x => x.UpdateData != null && x.UpdateData.Translation != null, () =>
            {
                RuleFor(x => x.UpdateData!.Translation)
                    .NotEmpty()
                    .WithName("Translation");
            });

            // Status là enum nullable, không cần validate thêm nếu chỉ cần accept null.
            // Nếu bạn muốn giới hạn chỉ cho phép Active/Inactive/Deleted:
            When(x => x.UpdateData != null && x.UpdateData.Status.HasValue, () =>
            {
                RuleFor(x => x.UpdateData!.Status!.Value)
                    .IsInEnum()
                    .WithName("Status");
            });
        }
    }
}
