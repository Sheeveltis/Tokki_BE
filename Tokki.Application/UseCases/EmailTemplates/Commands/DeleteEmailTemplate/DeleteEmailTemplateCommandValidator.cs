using FluentValidation;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate
{
    public class DeleteEmailTemplateCommandValidator : AbstractValidator<DeleteEmailTemplateCommand>
    {
        public DeleteEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithName("ID mẫu email");
        }
    }
}