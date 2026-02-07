using FluentValidation;

namespace Tokki.Application.UseCases.Passages.Commands.UpdatePassage
{
    public class UpdatePassageCommandValidator : AbstractValidator<UpdatePassageCommand>
    {
        public UpdatePassageCommandValidator()
        {
            RuleFor(x => x.PassageId)
                .NotEmpty()
                .WithName("Mã đoạn văn");

            RuleFor(x => x.Title)
                .MaximumLength(255)
                .When(x => !string.IsNullOrWhiteSpace(x.Title))
                .WithName("Tiêu đề");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(255)
                .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
                .WithName("Link media");
        }
    }
}
