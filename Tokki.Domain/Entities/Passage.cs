using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Passage
    {
        [Key]
        [MaxLength(10)]
        public string PassageId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public PassageStatus Status { get; set; } = PassageStatus.Active;

        public PassageMediaType MediaType { get; set; } = PassageMediaType.Text;

        // Navigation
        public virtual ICollection<QuestionBank> QuestionBank { get; set; } = new List<QuestionBank>();
    }
}
