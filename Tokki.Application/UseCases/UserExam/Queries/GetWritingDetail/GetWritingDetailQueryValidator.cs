using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetWritingDetail
{
    public class GetWritingDetailQueryValidator : AbstractValidator<GetWritingDetailQuery>
    {
        public GetWritingDetailQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
