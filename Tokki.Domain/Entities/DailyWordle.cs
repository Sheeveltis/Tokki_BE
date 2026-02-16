using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class DailyWordle
    {
        [Key]
        [MaxLength(20)] 
        public string DailyWordleId { get; set; } = string.Empty;

        public DateOnly GameDate { get; set; }

        public WordleLevel Level { get; set; }

        [MaxLength(10)]
        public string Word { get; set; } = string.Empty;

        [MaxLength(15)]
        public string VocabularyId { get; set; } = string.Empty;

        [ForeignKey("VocabularyId")]
        public virtual Vocabulary? Vocabulary { get; set; }
    }
}
