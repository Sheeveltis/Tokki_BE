using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class QuestionType
    {
        [Key]
        [MaxLength(10)]
        public string QuestionTypeId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public QuestionSkill Skill { get; set; } 

        [Required]
        public DifficultyLevel Difficulty { get; set; } 

        [Required]
        public ExamType ExamType { get; set; } 

        public bool IsActive { get; set; } = true;
        public int OrderIndex { get; set; } = 0;

        public virtual ICollection<QuestionBank> QuestionBank { get; set; } = new List<QuestionBank>();
    }
}