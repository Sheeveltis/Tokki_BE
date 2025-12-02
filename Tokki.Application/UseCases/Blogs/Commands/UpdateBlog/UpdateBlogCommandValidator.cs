using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.UpdateBlog
{
    public class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
    {
        public UpdateBlogCommandValidator()
        {
            RuleFor(x => x.Id)
              .NotEmpty()
              .WithName("ID blog");

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề");

            RuleFor(x => x.Slug)
                .Matches(@"^[a-z0-9-]+$")
                .When(x => !string.IsNullOrEmpty(x.Slug))
                .WithMessage("Slug chỉ được chứa chữ thường, số và dấu gạch ngang.");

            RuleFor(x => x.ShortDescription)
                .NotEmpty()
                .MaximumLength(500)
                .WithName("Mô tả ngắn");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithName("Nội dung");

            RuleFor(x => x.CategoryId)
                .NotEmpty()
                .WithName("Danh mục");

            RuleFor(x => x.ThumbnailUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.ThumbnailUrl))
                .WithMessage("Đường dẫn ảnh không hợp lệ.");

            RuleForEach(x => x.Tags)
                .NotEmpty()
                .MaximumLength(50)
                .WithName("Tên thẻ");
        }
    }
}
