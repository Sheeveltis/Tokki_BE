using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetReadingDetail
{
    public class GetReadingDetailQueryValidator : AbstractValidator<GetReadingDetailQuery>
    {
        public GetReadingDetailQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
