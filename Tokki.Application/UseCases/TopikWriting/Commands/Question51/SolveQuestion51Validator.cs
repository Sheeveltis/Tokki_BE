using FluentValidation;

namespace Tokki.Application.UseCases.TopikWriting.Question51.Commands
{
    public sealed class SolveQuestion51Validator : AbstractValidator<SolveQuestion51Command>
    {
        public SolveQuestion51Validator()
        {
            RuleFor(x => x.Payload.UserExamWritingAnswerId)
                .NotEmpty()
                .WithMessage("UserExamWritingAnswerId không được để trống.");
        }
    }
}