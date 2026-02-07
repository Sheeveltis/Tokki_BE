using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic
{
    public class CompleteTopicCommandValidator : AbstractValidator<CompleteTopicCommand>
    {
        public CompleteTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .MaximumLength(15)
                .WithName("topicId");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Không tìm thấy thông tin người dùng (UserId).");
        }
    }
}
