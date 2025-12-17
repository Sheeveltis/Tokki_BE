using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    // Bảng trung gian: phân loại vocabulary vào các topic
    public class VocabularyTopic
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        public VocabularyTopicStatus Status { get; set; } = VocabularyTopicStatus.Active;

        public virtual Vocabulary Vocabulary { get; set; } = null!;
        public virtual Topic Topic { get; set; } = null!;
    }
}