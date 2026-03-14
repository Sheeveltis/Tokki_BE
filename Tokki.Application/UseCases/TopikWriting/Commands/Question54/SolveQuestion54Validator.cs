// Application/UseCases/TopikWriting/Question54/Commands/SolveQuestion54Validator.cs
using FluentValidation;

namespace Tokki.Application.UseCases.TopikWriting.Question54.Commands
{
    public sealed class SolveQuestion54Validator : AbstractValidator<SolveQuestion54Command>
    {
        public SolveQuestion54Validator()
        {
            RuleFor(x => x.Payload.UserExamWritingAnswerId)
                .NotEmpty()
                .WithMessage("UserExamWritingAnswerId không được để trống.");
        }
    }
}