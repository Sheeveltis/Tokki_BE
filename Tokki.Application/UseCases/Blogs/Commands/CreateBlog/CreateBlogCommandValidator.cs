using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateBlog
{
    public class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
    {
        public CreateBlogCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255)
                .WithName("Tiêu đề");

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
                .WithMessage("Đường dẫn ảnh thumbnail không hợp lệ.");

            RuleForEach(x => x.Tags)
                 .NotEmpty()     
                 .MaximumLength(50)
                 .WithName("Tên thẻ");
        }
    }
}
