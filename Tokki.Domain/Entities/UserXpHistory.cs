using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class UserXpHistory
    {
        [Key]
        [MaxLength(21)] 
        public string Id { get; set; }

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; }

        public long Amount { get; set; } 

        public XpSource Action { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [ForeignKey("UserId")]
        public virtual Account Account { get; set; }
    }
}