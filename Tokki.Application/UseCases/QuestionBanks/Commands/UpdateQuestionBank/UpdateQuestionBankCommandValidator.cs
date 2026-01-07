using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandValidator : AbstractValidator<UpdateQuestionBankCommand>
    {
        public UpdateQuestionBankCommandValidator()
        {
            RuleFor(x => x.QuestionBankId)
                .NotEmpty()
                .WithName("Mã câu hỏi");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithName("Nội dung câu hỏi");
        }
    }
}