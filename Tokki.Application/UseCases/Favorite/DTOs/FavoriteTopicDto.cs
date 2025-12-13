using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.DTOs
{

    public class FavoriteTopicDto
    {
        public string FavoriteTopicId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Note { get; set; }
        public int WordCount { get; set; }
        public DateTime CreateDate { get; set; }
        public UserFavoriteTopicStatus Status { get; set; }
    }
}
