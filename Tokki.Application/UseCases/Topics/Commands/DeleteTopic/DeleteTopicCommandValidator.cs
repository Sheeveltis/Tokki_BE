using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.DeleteTopic
{
    public class DeleteTopicCommandValidator : AbstractValidator<DeleteTopicCommand>
    {
        public DeleteTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .MaximumLength(15)
                .WithName("Mã chủ đề");
        }
    }
}
