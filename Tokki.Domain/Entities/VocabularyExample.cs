using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class VocabularyExample
    {
        [Key]
        public string ExampleId { get; set; } = string.Empty;

        [ForeignKey("Vocabulary")]
        public string VocabularyId { get; set; } = string.Empty;

        /// <summary>
        /// Câu ví dụ
        /// </summary>
        public string Sentence { get; set; } = string.Empty;

        /// <summary>
        /// Bản dịch câu ví dụ
        /// </summary>
        public string? Translation { get; set; }

        // Audit Fields
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public string CreateBy { get; set; } = string.Empty;
        public VocabularyExampleStatus Status { get; set; } = VocabularyExampleStatus.Active; 
        // Navigation Property
        public virtual Vocabulary Vocabulary { get; set; } = null!;
    }
}