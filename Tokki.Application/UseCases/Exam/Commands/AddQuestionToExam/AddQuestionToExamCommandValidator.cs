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
            // 1. ExamId
            // Sử dụng: NotEmptyValidator
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithName("ID bài test");

            // 2. QuestionBankId
            // Sử dụng: NotEmptyValidator
            RuleFor(x => x.QuestionBankId)
                .NotEmpty()
                .WithName("ID câu hỏi");

            // 3. QuestionNo
            // Sử dụng: GreaterThanValidator
            // Thông báo sẽ tự động thành: "'Số thứ tự câu hỏi' phải lớn hơn '0'."
            RuleFor(x => x.QuestionNo)
                .GreaterThan(0)
                .WithName("Số thứ tự câu hỏi");

            // 4. Score
            // Sử dụng: GreaterThanValidator
            // Thông báo sẽ tự động thành: "'Điểm số' phải lớn hơn '0'."
            RuleFor(x => x.Score)
                .GreaterThan(0)
                .WithName("Điểm số");
        }
    }
}
