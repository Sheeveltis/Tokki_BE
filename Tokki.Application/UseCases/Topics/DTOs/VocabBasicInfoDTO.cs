using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class VocabBasicInfoDTO
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string? ImgURL { get; set; }
        public string? AudioUrl { get; set; }
    }
}
