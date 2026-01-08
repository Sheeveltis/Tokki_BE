using FluentValidation;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommandValidator : AbstractValidator<UpdateExamTemplateCommand>
    {
        public UpdateExamTemplateCommandValidator()
        {
            RuleFor(x => x.ExamTemplateId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(255);
            RuleFor(x => x.Type).IsInEnum();
        }
    }
}