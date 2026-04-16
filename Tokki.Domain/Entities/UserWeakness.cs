using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class UserWeakness
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string QuestionTypeId { get; set; } = string.Empty;

        public string? RoadmapId { get; set; }

        public int Status { get; set; } 
        public double? InitialScore { get; set; }
        public double? CurrentScore { get; set; }
        public int Priority { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("QuestionTypeId")]
        public virtual QuestionType QuestionType { get; set; }
    }
}