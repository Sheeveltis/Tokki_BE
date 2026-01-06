using FluentValidation;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.CreateQuestionType
{
    public class CreateQuestionTypeCommandValidator : AbstractValidator<CreateQuestionTypeCommand>
    {
        public CreateQuestionTypeCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Tên loại câu hỏi");
            RuleFor(x => x.Code).MaximumLength(50).WithName("Mã code");
            RuleFor(x => x.Skill).IsInEnum().WithName("Kỹ năng");
            RuleFor(x => x.Difficulty).IsInEnum().WithName("Độ khó");
            RuleFor(x => x.ExamType).IsInEnum().WithName("Loại đề thi");
        }
    }
}