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

            // Cho phép client gửi "" để "không đổi", nên không validate NotEmpty nữa.

            When(x => x.UpdateData != null && x.UpdateData.Status.HasValue, () =>
            {
                RuleFor(x => x.UpdateData!.Status!.Value)
                    .IsInEnum()
                    .WithName("Status");
            });
        }
    }
}
