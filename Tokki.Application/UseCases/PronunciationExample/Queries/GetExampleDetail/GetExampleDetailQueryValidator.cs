using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail
{
    public class GetExampleDetailQueryValidator : AbstractValidator<GetExampleDetailQuery>
    {
        public GetExampleDetailQueryValidator()
        {
            RuleFor(x => x.ExampleId)
                .NotEmpty().WithMessage("ExampleId không được để trống.")
                .NotNull().WithMessage("ExampleId không hợp lệ.");
        }
    }
}
