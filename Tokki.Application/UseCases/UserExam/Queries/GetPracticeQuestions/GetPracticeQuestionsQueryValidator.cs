using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.Queries.GetPracticeQuestions
{
    public class GetPracticeQuestionsQueryValidator : AbstractValidator<GetPracticeQuestionsQuery>
    {
        public GetPracticeQuestionsQueryValidator()
        {
            RuleFor(x => x.QuestionTypeId)
                .NotEmpty().WithMessage("ID loại câu hỏi không được để trống.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng câu hỏi luyện tập phải lớn hơn 0.")
                .LessThanOrEqualTo(50).WithMessage("Mỗi lần luyện tập tối đa 50 câu.");
        }
    }
}
