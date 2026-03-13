using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.Commands.SyncMCQProgress
{
    public class SyncMCQProgressCommandValidator : AbstractValidator<SyncMCQProgressCommand>
    {
        public SyncMCQProgressCommandValidator()
        {
            RuleFor(x => x.Answers)
                .NotEmpty().WithMessage("Danh sách đáp án không được để trống.")
                .Must(x => x != null && x.Count <= 100).WithMessage("Mỗi lần đồng bộ không quá 100 câu để đảm bảo hiệu năng.");

            RuleForEach(x => x.Answers).ChildRules(answer =>
            {
                answer.RuleFor(a => a.UserQuestionId)
                    .NotEmpty().WithMessage("ID câu hỏi người dùng không được để trống.");

                answer.RuleFor(a => a.SelectedOptionId)
                    .Must(id => id == null || !string.IsNullOrWhiteSpace(id))
                    .WithMessage("ID lựa chọn không hợp lệ.");
            });
        }
    }
}
