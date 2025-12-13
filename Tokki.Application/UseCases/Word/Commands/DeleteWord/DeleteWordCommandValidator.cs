
using FluentValidation;

namespace Tokki.Application.UseCases.Word.Commands.DeleteWord
{
    public class DeleteWordCommandValidator : AbstractValidator<DeleteWordCommand>
    {
        public DeleteWordCommandValidator()
        {
            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("ID từ vựng không được để trống.");
        }
    }
}
