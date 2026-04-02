using FluentValidation;

namespace Tokki.Application.UseCases.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Mã danh mục là bắt buộc.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
                .MaximumLength(100).WithMessage("Tên danh mục không vượt quá 100 ký tự.");
        }
    }
}
