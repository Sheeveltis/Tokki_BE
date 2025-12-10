using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class AccountTitle
    {
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        public int TitleId { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public virtual Account Account { get; set; }
        public virtual Title Title { get; set; }
    }
}