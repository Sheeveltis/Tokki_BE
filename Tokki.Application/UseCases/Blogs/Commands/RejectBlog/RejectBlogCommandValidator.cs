using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.RejectBlog
{
    public class RejectBlogCommandValidator : AbstractValidator<RejectBlogCommand>
    {
        public RejectBlogCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty()
                .WithName("BlogId");

            RuleFor(x => x.RejectReason)
                .NotEmpty().WithMessage(" là bắt buộc.")
                .MinimumLength(10)
                .MaximumLength(2000)
                .WithName("Lý do từ chối");
        }
    }
}
