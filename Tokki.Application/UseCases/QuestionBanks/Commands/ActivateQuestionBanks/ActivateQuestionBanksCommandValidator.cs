using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks
{

    public class ActivateQuestionBanksCommandValidator : AbstractValidator<ActivateQuestionBanksCommand>
    {
        public ActivateQuestionBanksCommandValidator()
        {
            RuleFor(x => x.QuestionBankIds)
                .NotNull()
                .WithName("Danh sách câu hỏi");

            RuleFor(x => x.QuestionBankIds)
                .NotEmpty()
                .WithName("Danh sách câu hỏi");

            RuleForEach(x => x.QuestionBankIds).ChildRules(id =>
            {
                id.RuleFor(v => v)
                    .NotEmpty()
                    .WithName("QuestionBankId");

                id.RuleFor(v => v)
                    .MaximumLength(10)
                    .WithName("QuestionBankId");
            });
        }
    }
}
