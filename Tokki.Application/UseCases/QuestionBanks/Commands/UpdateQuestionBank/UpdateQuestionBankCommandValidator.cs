using FluentValidation;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandValidator : AbstractValidator<UpdateQuestionBankCommand>
    {
        public UpdateQuestionBankCommandValidator()
        {
            RuleFor(x => x.QuestionBankId)
                .NotEmpty()
                .WithName("Mã câu hỏi");
        }
    }
}
