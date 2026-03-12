// Application/UseCases/TopikWriting/Question52/Commands/SolveQuestion52Validator.cs
using FluentValidation;

namespace Tokki.Application.UseCases.TopikWriting.Question52.Commands
{
    public sealed class SolveQuestion52Validator : AbstractValidator<SolveQuestion52Command>
    {
        public SolveQuestion52Validator()
        {
            RuleFor(x => x.Payload.UserExamWritingAnswerId)
                .NotEmpty()
                .WithMessage("UserExamWritingAnswerId không được để trống.");
        }
    }
}