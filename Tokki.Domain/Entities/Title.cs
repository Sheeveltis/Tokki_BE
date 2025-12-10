using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Title
    {
        [Key]
        public int TitleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; 

        [MaxLength(255)]
        public string? Description { get; set; } 

        public long RequiredXP { get; set; } = 0; 

        public bool IsSystemGiven { get; set; } = false; 

        [MaxLength(20)]
        public string ColorHex { get; set; } = "#000000"; 

        public string? IconUrl { get; set; }
        public TitleStatus Status { get; set; } = TitleStatus.Active;
    }
}