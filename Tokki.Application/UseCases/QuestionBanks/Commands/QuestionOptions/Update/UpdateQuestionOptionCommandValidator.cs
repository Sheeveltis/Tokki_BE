using FluentValidation;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update
{
    public class UpdateQuestionOptionCommandValidator : AbstractValidator<UpdateQuestionOptionCommand>
    {
        public UpdateQuestionOptionCommandValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    !string.IsNullOrWhiteSpace(x.KeyOption) ||
                    !string.IsNullOrWhiteSpace(x.Content) ||
                    !string.IsNullOrWhiteSpace(x.ImageUrl) ||
                    x.IsCorrect.HasValue
                )
                .WithMessage("Không có dữ liệu cập nhật.");

            When(x => !string.IsNullOrWhiteSpace(x.KeyOption), () =>
            {
                RuleFor(x => x.KeyOption!)
                    .Must(k => k is "1" or "2" or "3" or "4")
                    .WithName("KeyOption")
                    .WithMessage("KeyOption phải là '1', '2', '3' hoặc '4'.");
            });

         }
    }
}
