using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteWord
{
    public class AddFavoriteWordCommandValidator : AbstractValidator<AddFavoriteWordCommand>
    {
        public AddFavoriteWordCommandValidator()
        {
            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("ID từ vựng không được để trống.");
        }
    }
}
