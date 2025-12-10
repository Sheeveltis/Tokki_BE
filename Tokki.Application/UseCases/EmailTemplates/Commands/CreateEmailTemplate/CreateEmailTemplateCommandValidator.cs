using FluentValidation;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate
{
    public class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
    {
        public CreateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateKey)
                .NotEmpty() 
                .MaximumLength(100) 
                .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("'{PropertyName}' chỉ được chứa chữ cái không dấu, số, dấu gạch ngang (-) hoặc gạch dưới (_).")
                .WithName("Mã mẫu email");

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