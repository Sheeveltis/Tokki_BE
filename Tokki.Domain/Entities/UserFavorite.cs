using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{

    public class UserFavorite
    {
        [Key]
        public string FavoriteId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string VocabId { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }

        // Navigation properties
        public virtual Account User { get; set; } = null!;
        public virtual Vocabulary Vocabulary { get; set; } = null!;
    }
}
