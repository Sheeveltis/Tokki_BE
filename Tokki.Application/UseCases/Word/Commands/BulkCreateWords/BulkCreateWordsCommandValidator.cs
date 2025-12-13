using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Word.Commands.BulkCreateWords
{
    public class BulkCreateWordsCommandValidator : AbstractValidator<BulkCreateWordsCommand>
    {
        public BulkCreateWordsCommandValidator()
        {
            RuleFor(x => x.Words)
                .NotEmpty().WithMessage("Danh sách từ vựng không được để trống.")
                .Must(words => words.Count <= 100).WithMessage("Chỉ được tạo tối đa 100 từ vựng trong một lần.")
                .WithName("Danh sách từ vựng");

            RuleForEach(x => x.Words).ChildRules(word =>
            {
                word.RuleFor(w => w.Text)
                    .NotEmpty()
                    .MaximumLength(255)
                    .WithName("Từ vựng");

                word.RuleFor(w => w.Pronunciation)
                    .MaximumLength(255)
                    .WithName("Phiên âm");

                word.RuleFor(w => w.Meanings)
                    .NotEmpty().WithMessage("Mỗi từ phải có ít nhất một nghĩa.")
                    .WithName("Danh sách nghĩa");

                word.RuleForEach(w => w.Meanings).ChildRules(meaning =>
                {
                    meaning.RuleFor(m => m.Definition)
                        .NotEmpty()
                        .WithName("Định nghĩa");

                    meaning.RuleFor(m => m.ImgURL)
                        .MaximumLength(500)
                        .WithName("URL hình ảnh");
                });
            });
        }
    }
}
