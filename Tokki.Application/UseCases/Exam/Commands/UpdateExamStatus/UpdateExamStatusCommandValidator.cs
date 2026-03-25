using FluentValidation;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamStatus
{
    public class UpdateExamStatusCommandValidator : AbstractValidator<UpdateExamStatusCommand>
    {
        public UpdateExamStatusCommandValidator()
        {
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithName("Id đề thi");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Trạng thái đề thi không hợp lệ.");
        }
    }
}
