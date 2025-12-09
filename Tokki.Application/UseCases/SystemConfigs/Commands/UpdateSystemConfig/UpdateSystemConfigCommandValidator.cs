using FluentValidation;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;

namespace Tokki.Application.UseCases.SystemConfigs.Validators
{
    public class UpdateSystemConfigCommandValidator : AbstractValidator<UpdateSystemConfigCommand>
    {
        public UpdateSystemConfigCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Value)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Value));

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}