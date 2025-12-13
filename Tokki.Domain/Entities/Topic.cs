using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Topic
    {
        [Key]
        public string TopicId { get; set; } = string.Empty;

        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        public TopicStatus Status { get; set; } = TopicStatus.Active;

        public virtual ICollection<MeaningTopic> MeaningTopics { get; set; } = new List<MeaningTopic>();
        public virtual ICollection<UserFavoriteTopic> UserFavorites { get; set; } = new List<UserFavoriteTopic>();

    }
}
