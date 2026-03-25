using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
    {
        public CreateExamCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithName("tên đề thi");

            RuleFor(x => x.ExamTemplateId)
                .NotEmpty().WithMessage("Phải chọn cấu trúc đề thi (ExamTemplate).");

            RuleFor(x => x.SkillDurations)
                .NotEmpty().WithMessage("Vui lòng nhập thời gian cho các kỹ năng.");

            RuleForEach(x => x.SkillDurations)
                .ChildRules(skill => 
                {
                    skill.RuleFor(s => s.Value)
                         .GreaterThan(0).WithMessage("Thời gian của kỹ năng '{PropertyName}' phải lớn hơn 0.");
                });
        }
    }
}
