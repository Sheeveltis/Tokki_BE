using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class UserFavoriteWord
    {
        [Key]
        public string FavoriteWordId { get; set; } = string.Empty; 

        public string UserId { get; set; } = string.Empty;
        public string WordId { get; set; } = string.Empty;
        public string? MeaningId { get; set; }
        public string? Note { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public UserFavoriteWordStatus Status { get; set; } = UserFavoriteWordStatus.Active;

        public virtual Word Word { get; set; } = null!;
        public virtual Meaning? Meaning { get; set; }
    }
}
