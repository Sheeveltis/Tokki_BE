using FluentValidation;
using System.Linq;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank
{
    public class RejectQuestionBanksCommandValidator : AbstractValidator<RejectQuestionBanksCommand>
    {
        public RejectQuestionBanksCommandValidator()
        {
            RuleFor(x => x.QuestionBankIds)
                .NotNull()
                .WithMessage("Danh sách mã câu hỏi là bắt buộc.")
                .Must(ids => ids.Any(i => !string.IsNullOrWhiteSpace(i)))
                .WithMessage("Danh sách mã câu hỏi không được rỗng.");

            RuleFor(x => x.QuestionBankIds)
                .Must(ids =>
                {
                    if (ids == null) return true;

                    var normalized = ids
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .Select(i => i.Trim())
                        .ToList();

                    return normalized.Distinct().Count() == normalized.Count;
                })
                .WithMessage("Danh sách mã câu hỏi bị trùng.");

            RuleFor(x => x.RejectReason)
                .NotEmpty()
                .WithName("Lý do từ chối");
        }
    }
}
