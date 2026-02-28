using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class ExamTemplateStructure
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string StructureHash { get; set; } = string.Empty;

        [Required]
        public string ExamTemplateId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ExamTemplateId")]
        public virtual ExamTemplate ExamTemplate { get; set; }
    }
}