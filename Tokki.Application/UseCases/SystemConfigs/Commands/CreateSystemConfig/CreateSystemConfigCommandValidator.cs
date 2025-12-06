using FluentValidation;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Create
{
    public class CreateSystemConfigCommandValidator : AbstractValidator<CreateSystemConfigCommand>
    {
        public CreateSystemConfigCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Key không được để trống")
                .MaximumLength(100).WithMessage("Key tối đa 100 ký tự")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Key chỉ chứa chữ, số và dấu gạch dưới");
        }
    }
}