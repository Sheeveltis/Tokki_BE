using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam
{
    public class SubmitUserExamCommandValidator : AbstractValidator<SubmitUserExamCommand>
    {
        public SubmitUserExamCommandValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
