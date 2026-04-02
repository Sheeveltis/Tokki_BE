using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult
{
    public class GetUserExamResultQueryValidator : AbstractValidator<GetUserExamResultQuery>
    {
        public GetUserExamResultQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
