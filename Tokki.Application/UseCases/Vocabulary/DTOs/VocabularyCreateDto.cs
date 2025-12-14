using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    /// <summary>
    /// DTO để tạo vocabulary mới
    /// Bao gồm từ, phát âm, nghĩa, và các thông tin liên quan
    /// </summary>
    public class VocabularyCreateDto
    {
        /// <summary>
        /// Từ gốc (ví dụ: "은행")
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Phát âm (ví dụ: "eunhaeng")
        /// </summary>
        public string? Pronunciation { get; set; }

        /// <summary>
        /// Nghĩa của từ (ví dụ: "ngân hàng" hoặc "quả ngân hạnh")
        /// </summary>
        public string Definition { get; set; } = string.Empty;

        /// <summary>
        /// Câu ví dụ sử dụng từ này
        /// </summary>
        public string? ExampleSentence { get; set; }

        /// <summary>
        /// URL hình ảnh minh họa
        /// </summary>
        public string? ImgURL { get; set; }

        /// <summary>
        /// Danh sách TopicId mà vocabulary này thuộc về
        /// </summary>
        public List<string> TopicIds { get; set; } = new();
    }
}
