using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.ApproveBlog
{
    public class ApproveBlogCommandValidator : AbstractValidator<ApproveBlogCommand>
    {
        public ApproveBlogCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty()
                .WithName("BlogId");


        }
    }
}
