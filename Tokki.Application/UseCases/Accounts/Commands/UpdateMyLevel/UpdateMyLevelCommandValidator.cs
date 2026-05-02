using FluentValidation;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel
{
    public class UpdateMyLevelCommandValidator : AbstractValidator<UpdateMyLevelCommand>
    {
        public UpdateMyLevelCommandValidator(IEnumConfigRepository enumConfigRepository)
        {
            RuleFor(x => x.Level)
                .MustAsync(async (level, cancellation) =>
                {
                    if (level == null) return true;
                    return await enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == EnumGroup.TopicLevel && x.Value == level.Value && x.IsActive) != null;
                })
                .WithName("Cấp độ")
                .WithMessage("Cấp độ TOPIK không hợp lệ hoặc không tồn tại.");
        }
    }
}
