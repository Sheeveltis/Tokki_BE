using FluentValidation;

namespace Tokki.Application.UseCases.Gamification.Commands.AddGameXp
{
    public class AddGameXpCommandValidator : AbstractValidator<AddGameXpCommand>
    {
        public AddGameXpCommandValidator()
        {
            RuleFor(x => x.Amount)
                .InclusiveBetween(1, 500).WithMessage("Số XP không hợp lệ. Vui lòng nhập giá trị từ 1 đến 500.");
            
            RuleFor(x => x.Source)
                .IsInEnum().WithMessage("Nguồn XP không hợp lệ.");
        }
    }
}
