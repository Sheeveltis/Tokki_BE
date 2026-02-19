using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Grammar
    {
        [Key]
        public string GrammarId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Title { get; set; }

        [Required]
        public string Syntaxes { get; set; } 

        public string? Description { get; set; } 

        public CurrentTopikLevel Level { get; set; } 

        public string? RelatedQuestionTypeId { get; set; }

        [ForeignKey("RelatedQuestionTypeId")]
        public virtual QuestionType? RelatedQuestionType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}