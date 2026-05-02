using FluentValidation;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Create
{
    public class CreateSystemConfigCommandValidator : AbstractValidator<CreateSystemConfigCommand>
    {
        public CreateSystemConfigCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(100)
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Mã cấu hình chỉ được chứa chữ cái, số và dấu gạch dưới.")
                .WithName("Mã cấu hình");

            RuleFor(x => x.Value)
                .MaximumLength(4000)
                .Must((command, value) =>
                {
                    if (command.Key == "AI_PRONUNCIATION_PROMPT" && !string.IsNullOrEmpty(value))
                    {
                        return value.Contains("{targetText}") &&
                               value.Contains("{ruleContext}") &&
                               value.Contains("{detailedInfo}");
                    }
                    if (command.Key == "AI_WORDLE_PROMPT" && !string.IsNullOrEmpty(value))
                    {
                        return value.Contains("{word}") &&
                               value.Contains("{definition}") &&
                               value.Contains("{sentence}");
                    }
                    return true;
                })
                .WithMessage(command => command.Key == "AI_PRONUNCIATION_PROMPT" 
                    ? "Prompt phát âm phải chứa đầy đủ các biến: {targetText}, {ruleContext}, {detailedInfo}" 
                    : "Prompt Wordle phải chứa đầy đủ các biến: {word}, {definition}, {sentence}")
                .WithName("Giá trị cấu hình");
        }
    }
}