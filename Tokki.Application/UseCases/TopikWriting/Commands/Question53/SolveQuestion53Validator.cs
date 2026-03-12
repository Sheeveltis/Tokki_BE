// Application/UseCases/TopikWriting/Question53/Commands/SolveQuestion53Validator.cs
using FluentValidation;

namespace Tokki.Application.UseCases.TopikWriting.Question53.Commands
{
    public sealed class SolveQuestion53Validator : AbstractValidator<SolveQuestion53Command>
    {
        public SolveQuestion53Validator()
        {
            RuleFor(x => x.Payload.UserExamWritingAnswerId)
                .NotEmpty()
                .WithMessage("UserExamWritingAnswerId không được để trống.");
        }
    }
}