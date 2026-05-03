using FluentValidation;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff
{
    public class CreateTopicByStaffCommandValidator
        : AbstractValidator<CreateTopicByStaffCommand>
    {
        public CreateTopicByStaffCommandValidator(IEnumConfigRepository enumConfigRepository)
        {
            RuleFor(x => x.TopicName)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên chủ đề");

            RuleFor(x => x.Description)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Level)
                .MustAsync(async (level, cancellation) =>
                {
                    return await enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == EnumGroup.TopicLevel && x.Value == level && x.IsActive) != null;
                })
                .WithMessage("Cấp độ không hợp lệ hoặc không tồn tại.");
        }
    }
}
