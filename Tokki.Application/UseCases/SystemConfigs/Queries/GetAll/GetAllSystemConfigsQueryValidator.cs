using FluentValidation;

namespace Tokki.Application.UseCases.SystemConfigs.Queries.GetAll
{
    public class GetAllSystemConfigsQueryValidator : AbstractValidator<GetAllSystemConfigsQuery>
    {
        public GetAllSystemConfigsQueryValidator()
        {
            // Nếu sau này có filter (ví dụ: IsActiveOnly), bạn thêm Rule vào đây.
        }
    }
}