using FluentValidation;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Update
{
    public class UpdateSystemConfigCommandValidator : AbstractValidator<UpdateSystemConfigCommand>
    {
        public UpdateSystemConfigCommandValidator()
        {
           
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(100)
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

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithName("Mô tả");
        }
    }
}