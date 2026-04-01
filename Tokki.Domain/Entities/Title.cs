using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Title
    {
        [Key]
        [MaxLength(21)] 
        public string TitleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; 

        [MaxLength(255)]
        public string? Description { get; set; } 

        [MaxLength(20)]
        public string ColorHex { get; set; } = "#000000"; 

        public string? IconUrl { get; set; }

        public TitleStatus Status { get; set; } = TitleStatus.Active;

        public TitleRequirementType RequirementType { get; set; } = TitleRequirementType.Level;

        public long RequirementQuantity { get; set; } = 0; 
    }
}