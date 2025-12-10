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
                .MaximumLength(2000) 
                .WithName("Giá trị cấu hình");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithName("Mô tả");
        }
    }
}