using FluentValidation;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateUserBlog
{
    public class CreateUserBlogCommandValidator : AbstractValidator<CreateUserBlogCommand>
    {
        public CreateUserBlogCommandValidator()
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
