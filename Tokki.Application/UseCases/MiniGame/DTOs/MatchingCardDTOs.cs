using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class MatchingCardDTO
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;         // Từ vựng
        public string Pronunciation { get; set; } = string.Empty; // Phiên âm
        public string Definition { get; set; } = string.Empty;    // Nghĩa (để ghép cặp)
        public string ImgURL { get; set; } = string.Empty;        // Hình ảnh
        public string AudioURL { get; set; } = string.Empty;      // Âm thanh (nếu cần nghe để chọn)
    }
}
