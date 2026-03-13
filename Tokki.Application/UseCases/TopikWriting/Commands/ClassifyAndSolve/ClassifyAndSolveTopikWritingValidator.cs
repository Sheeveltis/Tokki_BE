using FluentValidation;

namespace Tokki.Application.UseCases.TopikWriting.Commands.ClassifyAndSolve
{
    public sealed class ClassifyAndSolveTopikWritingValidator
        : AbstractValidator<ClassifyAndSolveTopikWritingCommand>
    {
        public ClassifyAndSolveTopikWritingValidator()
        {
            RuleFor(x => x.Payload.Level)
                .InclusiveBetween(1, 6)
                .WithName("Level");

            RuleFor(x => x.Payload.Question.No)
                .GreaterThan(0)
                .WithName("Question.No");

            // Cho phép submission rỗng nếu bạn muốn “chỉ phân loại”
            // Nhưng nếu mục tiêu là chấm bài thì nên bắt buộc:
            RuleFor(x => x.Payload.Question.Submission.Text)
                .NotEmpty()
                .WithMessage("Bạn chưa nhập bài viết.")
                .WithName("Submission.Text");
        }
    }
}
