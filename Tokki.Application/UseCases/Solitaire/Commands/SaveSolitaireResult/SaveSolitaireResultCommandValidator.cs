using FluentValidation;

namespace Tokki.Application.UseCases.Solitaire.Commands.SaveSolitaireResult
{
    public class SaveSolitaireResultCommandValidator : AbstractValidator<SaveSolitaireResultCommand>
    {
        public SaveSolitaireResultCommandValidator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty()
                    .WithMessage("GameId không được để trống.")
                .MaximumLength(15)
                    .WithMessage("GameId không được vượt quá 15 ký tự.")
                .WithName("Game");

            RuleFor(x => x.Score)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Điểm không hợp lệ.")
                .WithName("Điểm");

            RuleFor(x => x.GameDifficulty)
                .IsInEnum()
                    .WithMessage("Độ khó không hợp lệ.")
                .WithName("Độ khó");
        }
    }
}
