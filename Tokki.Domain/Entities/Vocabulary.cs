using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class Vocabulary
    {
        [Key]
        public string VocabId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? ExampleSentence { get; set; }
        public string? AudioURL { get; set; }

        // Navigation properties
        public virtual Topic Topic { get; set; } = null!;
        public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
    }
}
