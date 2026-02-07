using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create
{
    public class CreateQuestionOptionCommandValidator : AbstractValidator<CreateQuestionOptionCommand>
    {
        public CreateQuestionOptionCommandValidator()
        {
            RuleFor(x => x.KeyOption)
                .NotEmpty().WithName("KeyOption")
                .Must(k => k is "1" or "2" or "3" or "4")
                .WithMessage("KeyOption phải là '1', '2', '3' hoặc '4'.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .When(x => string.IsNullOrWhiteSpace(x.ImageUrl))
                .WithName("Nội dung đáp án")
                .WithMessage("Đáp án phải có nội dung text hoặc ảnh.");

            RuleFor(x => x.ImageUrl)
                .NotEmpty()
                .When(x => string.IsNullOrWhiteSpace(x.Content))
                .WithName("Ảnh đáp án")
                .WithMessage("Đáp án phải có nội dung text hoặc ảnh.");
        }
    }
}
