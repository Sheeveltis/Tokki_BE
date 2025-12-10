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
        }
    }
}