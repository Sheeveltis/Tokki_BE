using FluentValidation;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate
{
    public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
    {
        public UpdateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithName("ID mẫu email");

            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề email");

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithName("Nội dung email");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithName("Mô tả");
        }
    }
}