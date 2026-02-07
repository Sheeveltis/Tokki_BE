using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
namespace Tokki.Domain.Entities
{
    public class Vocabulary
    {
        [Key]
        public string VocabularyId { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string? Pronunciation { get; set; }

        // Nghĩa (ví dụ: "ngân hàng" hoặc "quả ngân hạnh")
        public string Definition { get; set; } = string.Empty;

        public string? ImgURL { get; set; }

        public string? AudioURL { get; set; }

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public VocabularyStatus Status { get; set; } = VocabularyStatus.Active;

        // Navigation Properties
        public virtual ICollection<VocabularyTopic> VocabularyTopics { get; set; } = new List<VocabularyTopic>();
        public virtual ICollection<UserFavoriteVocabulary> UserFavorites { get; set; } = new List<UserFavoriteVocabulary>();
        public virtual ICollection<VocabularyExample> VocabularyExamples { get; set; } = new List<VocabularyExample>();
    }
}