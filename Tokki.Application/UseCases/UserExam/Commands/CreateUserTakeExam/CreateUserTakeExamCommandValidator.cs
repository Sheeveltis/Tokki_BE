using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam
{
    public class CreateUserTakeExamCommandValidator : AbstractValidator<CreateUserTakeExamCommand>
    {
        public CreateUserTakeExamCommandValidator()
        {
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithName("Id đề thi");
        }
    }
}
