using FluentValidation;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;

namespace Tokki.Application.UseCases.EmailTemplates.Validators
{
    public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
    {
        public UpdateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .GreaterThan(0).WithMessage("TemplateId phải lớn hơn 0.");

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