using FluentValidation;

namespace Tokki.Application.UseCases.UserExam.Queries.GetListeningDetail
{
    public class GetListeningDetailQueryValidator : AbstractValidator<GetListeningDetailQuery>
    {
        public GetListeningDetailQueryValidator()
        {
            RuleFor(x => x.UserExamId)
                .NotEmpty()
                .WithName("Id phiên làm bài");
        }
    }
}
