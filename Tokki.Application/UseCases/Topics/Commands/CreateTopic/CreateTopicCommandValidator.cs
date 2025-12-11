using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopic
{
    public class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
    {
        public CreateTopicCommandValidator()
        {
            RuleFor(x => x.TopicName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên chủ đề");

            RuleFor(x => x.Description)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithName("Mô tả");

            RuleFor(x => x.CreateBy)
                .NotEmpty()
                .MaximumLength(15)
                .WithName("Người tạo");
        }
    }
}