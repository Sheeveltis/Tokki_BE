using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class UserExamAnswer
    {
        [Key]
        [StringLength(20)]
        public string UserExamAnswerId { get; set; }

        [Required]
        [StringLength(15)]
        public string UserExamId { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string QuestionId { get; set; } = null!;

        [StringLength(10)]
        public string? SelectedOptionId { get; set; }

        public int OrderIndex { get; set; }

        public bool? IsCorrect { get; set; }


        [ForeignKey("UserExamId")]
        public virtual UserExam UserExam { get; set; } = null!;

        [ForeignKey("QuestionId")]
        public virtual QuestionBank Question { get; set; } = null!;

        [ForeignKey("SelectedOptionId")]
        public virtual QuestionOption? SelectedOption { get; set; }
    }
}
