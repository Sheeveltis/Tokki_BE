using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyDetailForAdminDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? ImgURL { get; set; }
        public string? AudioURL { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        public VocabularyStatus Status { get; set; }

        public List<VocabularyTopicDto> Topics { get; set; } = new();

        public List<VocabularyExampleDetailDto> Examples { get; set; } = new();

    }
}
