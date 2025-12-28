using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel
{
    public class UpdateMyLevelCommandValidator : AbstractValidator<UpdateMyLevelCommand>
    {
        public UpdateMyLevelCommandValidator()
        {
            var validLevelsText = string.Join(", ",
          Enum.GetValues(typeof(TopicLevel))
              .Cast<TopicLevel>()
              .Select(v => $"{Convert.ToInt32(v)} ({v})"));

            RuleFor(x => x.Level)
                .Must(level => level == null || Enum.IsDefined(typeof(TopicLevel), level))
                .WithName("Cấp độ")
                .WithMessage($"'{{PropertyName}}' không hợp lệ. Giá trị hợp lệ: {validLevelsText}.");

        }
    }
}
