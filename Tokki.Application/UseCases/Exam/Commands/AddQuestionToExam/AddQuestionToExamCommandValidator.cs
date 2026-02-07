using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam
{
    using FluentValidation;

    public class AddQuestionToExamCommandValidator : AbstractValidator<AddQuestionToExamCommand>
    {
        public AddQuestionToExamCommandValidator()
        {
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithName("ID bài test");

            RuleFor(x => x.QuestionBankId)
                .NotEmpty()
                .WithName("ID câu hỏi");

            RuleFor(x => x.QuestionNo)
                .GreaterThan(0)
                .WithName("Số thứ tự câu hỏi");

            
        }
    }
}
