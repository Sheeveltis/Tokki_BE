using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    // Bảng trung gian mới
    public class MeaningTopic
    {
        public string MeaningId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        public MeaningTopicStatus Status { get; set; } = MeaningTopicStatus.Active;
        public virtual Meaning Meaning { get; set; } = null!;
        public virtual Topic Topic { get; set; } = null!;
    }
}
