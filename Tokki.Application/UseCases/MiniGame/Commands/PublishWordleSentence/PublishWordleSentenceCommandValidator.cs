using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.Commands.PublishWordleSentence
{
    public class PublishWordleSentenceCommandValidator : AbstractValidator<PublishWordleSentenceCommand>
    {
        public PublishWordleSentenceCommandValidator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty().WithMessage("SubmissionId không được để trống.");
        }
    }
}
