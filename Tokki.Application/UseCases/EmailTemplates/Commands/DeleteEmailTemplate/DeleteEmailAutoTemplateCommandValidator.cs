using FluentValidation;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate
{
    public class DeleteEmailAutoTemplateCommandValidator : AbstractValidator<DeleteEmailAutoTemplateCommand>
    {
        public DeleteEmailAutoTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithName("ID mẫu email");
        }
    }
}
