using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Meaning
    {
        [Key]
        public string MeaningId { get; set; } = string.Empty;
        public string WordId { get; set; } = string.Empty;

        public string Definition { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public string? ImgURL { get; set; }

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        public MeaningStatus Status { get; set; } = MeaningStatus.Active;

        public virtual Word Word { get; set; } = null!;
        public virtual ICollection<MeaningTopic> MeaningTopics { get; set; } = new List<MeaningTopic>();
        public virtual ICollection<UserFavoriteWord> UserFavorites { get; set; } = new List<UserFavoriteWord>();


    }
}
