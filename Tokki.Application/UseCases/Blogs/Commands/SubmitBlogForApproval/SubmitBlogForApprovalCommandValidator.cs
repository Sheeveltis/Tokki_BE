using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval
{
    public class SubmitBlogForApprovalCommandValidator : AbstractValidator<SubmitBlogForApprovalCommand>
    {
        public SubmitBlogForApprovalCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty()
                .WithName("BlogId");

        }
    }
}
