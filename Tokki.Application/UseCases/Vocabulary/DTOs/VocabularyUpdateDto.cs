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
        /// Câu ví dụ mới
        /// </summary>
        public string? ExampleSentence { get; set; }

        /// <summary>
        /// URL hình ảnh mới
        /// </summary>
        public string? ImgURL { get; set; }

        /// <summary>
        /// Danh sách TopicId mới (sẽ thay thế hoàn toàn danh sách cũ)
        /// - Nếu null: không cập nhật topics (giữ nguyên)
        /// - Nếu []: xóa tất cả topics (vocabulary không thuộc topic nào)
        /// - Nếu ["id1", "id2"]: thay thế bằng danh sách mới
        /// </summary>
        public List<string>? TopicIds { get; set; }
    }
}
