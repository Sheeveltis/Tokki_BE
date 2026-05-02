using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Topic
    {
        [Key]
        public string TopicId { get; set; } = string.Empty;

        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImgUrl { get; set; }

        public int Level { get; set; }

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        // Người duyệt + thời gian duyệt
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public TopicStatus Status { get; set; } = TopicStatus.Active;
        public TopicType TopicType { get; set; }

        public int? OrderIndex { get; set; }
        public virtual ICollection<VocabularyTopic> VocabularyTopics { get; set; } = new List<VocabularyTopic>();
    }
}
