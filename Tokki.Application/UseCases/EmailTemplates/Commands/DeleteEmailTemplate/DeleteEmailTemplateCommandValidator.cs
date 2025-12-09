using FluentValidation;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;

namespace Tokki.Application.UseCases.EmailTemplates.Validators
{
    public class DeleteEmailTemplateCommandValidator : AbstractValidator<DeleteEmailTemplateCommand>
    {
        public DeleteEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .GreaterThan(0).WithMessage("TemplateId phải lớn hơn 0.");
        }
    }
}