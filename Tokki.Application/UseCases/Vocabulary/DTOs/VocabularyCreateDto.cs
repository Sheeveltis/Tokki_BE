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
        /// Danh sách câu ví dụ sử dụng từ này
        /// </summary>
        public List<VocabularyExampleDto>? Examples { get; set; }

        /// <summary>
        /// URL hình ảnh minh họa
        /// </summary>
        public string? ImgURL { get; set; }

    }
}