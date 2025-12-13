using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExam
{
    public class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
    {
        public UpdateExamCommandValidator()
        {
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithName("ID bài test");

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(150)
                .WithName("Tiêu đề");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithName("Loại bài test");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithName("Trạng thái");
        }
    }
}
