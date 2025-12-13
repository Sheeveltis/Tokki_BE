using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{

    public class UserFavoriteTopic
    {
        [Key]
        public string FavoriteTopicId { get; set; } = string.Empty; // Bỏ Guid.NewGuid()

        public string UserId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public UserFavoriteTopicStatus Status { get; set; } = UserFavoriteTopicStatus.Active;

        public virtual Topic Topic { get; set; } = null!;
    }
}
