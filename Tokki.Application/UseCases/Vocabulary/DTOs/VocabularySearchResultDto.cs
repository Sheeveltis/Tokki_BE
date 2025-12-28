using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularySearchResultDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty; // Tiếng Hàn: 은행
        public string Definition { get; set; } = string.Empty; // Tiếng Việt: ngân hàng
        public string? Pronunciation { get; set; } // Phiên âm: eun-haeng
    }
}
