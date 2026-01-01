using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic
{
    public class ExportVocabByTopicQueryValidator : AbstractValidator<ExportVocabByTopicQuery>
    {
        public ExportVocabByTopicQueryValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .NotNull()
                .WithName("TopicId");
        }
    }
}
