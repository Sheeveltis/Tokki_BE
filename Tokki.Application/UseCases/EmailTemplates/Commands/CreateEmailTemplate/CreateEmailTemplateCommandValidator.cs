using FluentValidation;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;

namespace Tokki.Application.UseCases.EmailTemplates.Validators
{
    public class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
    {
        public CreateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateKey)
                .NotEmpty()
                .MaximumLength(100)
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("TemplateKey chỉ được chứa chữ, số, gạch dưới và gạch ngang.");

            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Body)
                .NotEmpty()
                .MaximumLength(10000);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}