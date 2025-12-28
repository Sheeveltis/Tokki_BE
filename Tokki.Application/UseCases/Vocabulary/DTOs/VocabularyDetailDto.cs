using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyDetailDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? ImgURL { get; set; }
        public string? AudioURL { get; set; }

        public VocabularyStatus Status { get; set; }

        public List<VocabularyTopicDto> Topics { get; set; } = new();
    }
}
