using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class EnumConfig
    {
        [Key]
        public int Id { get; set; }

        public EnumGroup GroupCode { get; set; }

        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public int Value { get; set; }

        [MaxLength(255)]
        public string Label { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}
