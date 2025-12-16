using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic
{
    public class AddVocabulariesToTopicCommandValidator : AbstractValidator<AddVocabulariesToTopicCommand>
    {
        public AddVocabulariesToTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty().WithMessage("ID chủ đề không được để trống.")
                .WithName("TopicId");

            RuleFor(x => x.VocabularyIds)
                .NotEmpty().WithMessage("Danh sách từ vựng không được để trống.")
                .Must(ids => ids != null && ids.Any()).WithMessage("Phải chọn ít nhất một từ vựng.")
                .WithName("Danh sách từ vựng");

            // Kiểm tra từng phần tử trong list
            RuleForEach(x => x.VocabularyIds)
                .NotEmpty().WithMessage("ID từ vựng không hợp lệ.");
        }
    }
}
