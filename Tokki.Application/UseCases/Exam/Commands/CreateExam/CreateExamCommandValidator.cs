using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Tokki.Application.UseCases.Exam.Commands.UpdateExam;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
    {
        public CreateExamCommandValidator()
        {
            RuleFor(x => x.ExamTemplateId)
                    .NotEmpty()
                    .WithName("Template bài test");

            RuleFor(x => x.Title)
                    .NotEmpty()
                    .MaximumLength(150)
                    .WithName("Tiêu đề");

            RuleFor(x => x.Type)
                    .IsInEnum()
                    .WithName("Loại bài test");

            RuleFor(x => x.Duration)
                    .IsInEnum()
                    .WithName("Thời gian");
        }
    }
}
