using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Queries
{
    public class GetBlogByIdQueryValidator : AbstractValidator<GetBlogByIdQuery>
    {
        public GetBlogByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithName("ID blog");
        }
    }
}
