using FluentValidation;

namespace Tokki.Application.UseCases.Games.Commands.UpdateGameResult
{
    public class UpdateGameResultCommandValidator : AbstractValidator<UpdateGameResultCommand>
    {
        public UpdateGameResultCommandValidator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty().WithMessage("GameId không được để trống.")
                .MaximumLength(15).WithMessage("GameId không được vượt quá 15 ký tự.")
                .WithName("Game");

            RuleFor(x => x.TopicId)
                .NotEmpty().WithMessage("TopicId không được để trống.")
                .MaximumLength(50).WithMessage("TopicId không được vượt quá 50 ký tự.")
                .WithName("Chủ đề");

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
