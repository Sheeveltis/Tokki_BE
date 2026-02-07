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

            RuleFor(x => x.Duration)
                .GreaterThan(0)
                .WithName("Thời gian làm bài");

            RuleFor(x => x.ExamTemplateId)
                .NotEmpty().WithMessage("Phải chọn cấu trúc đề thi (ExamTemplate).");
        }
    }
}
