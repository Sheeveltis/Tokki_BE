using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Word
    {
        [Key]
        public string WordId { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; } = string.Empty;
        public string? AudioURL { get; set; }

        // Audit Fields
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        // Dùng WordStatus
        public WordStatus Status { get; set; } = WordStatus.Active;

        // Navigation
        public virtual ICollection<Meaning> Meanings { get; set; } = new List<Meaning>();
        public virtual ICollection<UserFavoriteWord> UserFavorites { get; set; } = new List<UserFavoriteWord>();

    }
}
