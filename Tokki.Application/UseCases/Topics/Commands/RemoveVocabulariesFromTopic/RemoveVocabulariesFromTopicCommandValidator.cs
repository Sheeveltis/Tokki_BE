using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic
{
    using FluentValidation;

    namespace Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic
    {
        public class RemoveVocabulariesFromTopicCommandValidator
            : AbstractValidator<RemoveVocabulariesFromTopicCommand>
        {
            public RemoveVocabulariesFromTopicCommandValidator()
            {
                RuleFor(x => x.TopicId)
                    .NotEmpty().WithMessage("ID chủ đề không được để trống.")
                    .WithName("TopicId");

                RuleFor(x => x.VocabularyIds)
                    .NotEmpty().WithMessage("Danh sách từ vựng không được để trống.")
                    .Must(ids => ids != null && ids.Any())
                    .WithMessage("Phải chọn ít nhất một từ vựng.")
                    .WithName("Danh sách từ vựng");

                RuleForEach(x => x.VocabularyIds)
                    .NotEmpty().WithMessage("ID từ vựng không hợp lệ.");
            }
        }
    }

}
