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
            RuleFor(x => x.Status).IsInEnum();

            RuleFor(x => x.Parts).NotEmpty();
            RuleForEach(x => x.Parts).ChildRules(part =>
            {
                part.RuleFor(p => p.Skill).IsInEnum();
                part.RuleFor(p => p.QuestionFrom).GreaterThan(0);
                part.RuleFor(p => p.QuestionTo).GreaterThan(0);
                part.RuleFor(p => p.QuestionTo).GreaterThanOrEqualTo(p => p.QuestionFrom);
                part.RuleFor(p => p.Mark).GreaterThan(0);
                part.RuleFor(p => p.QuestionTypeId).NotEmpty();
            });
        }
    }
}