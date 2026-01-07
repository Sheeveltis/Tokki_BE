using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.CreatePassage
{
    public class CreatePassageCommandValidator : AbstractValidator<CreatePassageCommand>
    {
        public CreatePassageCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề");

            RuleFor(x => x.MediaType)
                .IsInEnum()
                .WithName("Loại media");

            // Text => cần Content
            RuleFor(x => x.Content)
                .NotEmpty()
                .When(x => x.MediaType == PassageMediaType.Text)
                .WithName("Nội dung");

            // Không phải Text => cần ImageUrl (Audio/Image)
            RuleFor(x => x.ImageUrl)
                .NotEmpty()
                .When(x => x.MediaType != PassageMediaType.Text)
                .WithName("Link media");
        }
    }
}
