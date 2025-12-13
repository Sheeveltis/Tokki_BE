using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<QuestionBank> QuestionBank { get; set; } = new List<QuestionBank>();
    }

}
