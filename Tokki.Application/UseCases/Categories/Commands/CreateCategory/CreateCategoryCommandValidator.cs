using FluentValidation;

namespace Tokki.Application.UseCases.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
                .MaximumLength(100).WithMessage("Tên danh mục không vượt quá 100 ký tự.");
        }
    }
}
