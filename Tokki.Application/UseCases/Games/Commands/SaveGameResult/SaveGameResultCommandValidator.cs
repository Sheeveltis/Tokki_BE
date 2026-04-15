using FluentValidation;

namespace Tokki.Application.UseCases.Games.Commands.SaveGameResult
{
    public class SaveGameResultCommandValidator : AbstractValidator<SaveGameResultCommand>
    {
        public SaveGameResultCommandValidator()
        {
            RuleFor(x => x.GameType)
                .IsInEnum()
                    .WithMessage("GameType không hợp lệ.")
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
