using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class SystemConfig
    {
        [Key]
        public int SystemConfigID { get; set; }
        [MaxLength(100)]
        public string Key { get; set; } = default!;
        public string? Value { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
        [MaxLength(50)]
        public string? DataType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
