using FluentValidation;
using Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart;

namespace Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart
{
    public class UpdateTemplatePartCommandValidator : AbstractValidator<UpdateTemplatePartCommand>
    {
        public UpdateTemplatePartCommandValidator()
        {
            RuleFor(x => x.TemplatePartId).NotEmpty();

            RuleFor(x => x.Skill).IsInEnum();

            RuleFor(x => x.QuestionFrom).GreaterThan(0);
            RuleFor(x => x.QuestionTo).GreaterThan(0);
            RuleFor(x => x.QuestionTo).GreaterThanOrEqualTo(x => x.QuestionFrom);

            RuleFor(x => x.PartTitle).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Mark).GreaterThan(0).WithName("Điểm số");

            RuleFor(x => x.QuestionTypeId).NotEmpty();
        }
    }
}