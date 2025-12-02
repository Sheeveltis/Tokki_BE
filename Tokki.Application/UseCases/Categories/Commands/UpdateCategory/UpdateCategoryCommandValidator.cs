using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithName("Mã danh mục");

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100) 
                .WithName("Tên danh mục");
        }
    }
}
