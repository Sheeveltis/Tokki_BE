using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam
{
    public class GetInProgressExamQueryValidator : AbstractValidator<GetInProgressExamQuery>
    {
        public GetInProgressExamQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
