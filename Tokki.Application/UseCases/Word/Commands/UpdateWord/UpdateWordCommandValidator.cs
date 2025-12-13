using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Word.Commands.UpdateWord
{
    public class UpdateWordCommandValidator : AbstractValidator<UpdateWordCommand>
    {
        public UpdateWordCommandValidator()
        {
            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("ID từ vựng không được để trống.");

            RuleFor(x => x.Text)
                .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Text))
                .WithName("Từ vựng");

            RuleFor(x => x.Pronunciation)
                .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Pronunciation))
                .WithName("Phiên âm");

            RuleForEach(x => x.Meanings)
                .ChildRules(meaning =>
                {
                    meaning.RuleFor(m => m.Definition)
                        .NotEmpty()
                        .WithName("Định nghĩa");

                    meaning.RuleFor(m => m.ImgURL)
                        .MaximumLength(500)
                        .WithName("URL hình ảnh");
                })
                .When(x => x.Meanings != null && x.Meanings.Any());
        }
    }


}
