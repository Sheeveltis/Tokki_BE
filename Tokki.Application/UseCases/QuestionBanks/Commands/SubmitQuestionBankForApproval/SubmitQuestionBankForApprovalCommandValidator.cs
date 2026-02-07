using FluentValidation;
using System.Linq;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval
{
    public class SubmitQuestionBankForApprovalCommandValidator
        : AbstractValidator<SubmitQuestionBankForApprovalCommand>
    {
        public SubmitQuestionBankForApprovalCommandValidator()
        {
            RuleFor(x => x.QuestionBankIds)
                .NotNull()
                .Must(ids => ids.Any(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage("Danh sách QuestionBankIds không hợp lệ.");

            RuleFor(x => x.QuestionBankIds)
                .Must(ids =>
                {
                    var normalized = ids
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .Select(i => i.Trim())
                        .ToList();

                    return normalized.Distinct().Count() == normalized.Count;
                })
                .WithMessage("Danh sách QuestionBankIds bị trùng.");
        }
    }
}
