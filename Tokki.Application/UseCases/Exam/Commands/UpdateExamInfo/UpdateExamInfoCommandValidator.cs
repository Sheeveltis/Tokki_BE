using FluentValidation;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamInfo
{
    public class UpdateExamInfoCommandValidator : AbstractValidator<UpdateExamInfoCommand>
    {
        public UpdateExamInfoCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithName("Tên đề thi")
                .MaximumLength(150);

            RuleFor(x => x.SkillDurations)
                .NotEmpty().WithMessage("Vui lòng cung cấp thời lượng cho các kỹ năng.");

            RuleForEach(x => x.SkillDurations)
                .ChildRules(skill => 
                {
                    skill.RuleFor(s => s.Value)
                         .GreaterThan(0).WithMessage("Thời lượng cho kỹ năng không được phép nhỏ hơn hoặc bằng 0.");
                });
        }
    }
}
