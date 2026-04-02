using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis
{
    public class GetExamAnalysisQueryValidator : AbstractValidator<GetExamAnalysisQuery>
    {
        public GetExamAnalysisQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
