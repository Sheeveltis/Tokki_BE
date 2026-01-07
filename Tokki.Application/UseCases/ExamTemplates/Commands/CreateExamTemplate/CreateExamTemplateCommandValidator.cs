using FluentValidation;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate
{
    public class CreateExamTemplateCommandValidator : AbstractValidator<CreateExamTemplateCommand>
    {
        public CreateExamTemplateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Tên mẫu đề thi");
            RuleFor(x => x.Description).MaximumLength(255).WithName("Mô tả");
            RuleFor(x => x.Type).IsInEnum().WithName("Loại đề");
        }
    }
}