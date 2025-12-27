using System.Collections.Generic;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyUpdateDto
    {
        /// <summary>
        /// Từ gốc mới (ví dụ: "은행" → "도서관")
        /// Nếu Text thay đổi, hệ thống sẽ TỰ ĐỘNG tạo lại file audio
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Phát âm mới
        /// </summary>
        public string? Pronunciation { get; set; }

        /// <summary>
        /// Nghĩa mới của từ
        /// </summary>
        public string? Definition { get; set; }


        /// <summary>
        /// URL hình ảnh mới
        /// </summary>
        public string? ImgURL { get; set; }

     
    }
}
