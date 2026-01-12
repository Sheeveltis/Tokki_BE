using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount
{
    public class IncreaseViewCountCommandValidator : AbstractValidator<IncreaseViewCountCommand>
    {
        public IncreaseViewCountCommandValidator()
        {
            RuleFor(x => x.BlogId)
                .NotEmpty()
                .WithName("BlogId");
        }
    }
}
