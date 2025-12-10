using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class UserXpHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; }

        public long Amount { get; set; } 

        [MaxLength(50)]
        public string Action { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [ForeignKey("UserId")]
        public virtual Account Account { get; set; }
    }
}