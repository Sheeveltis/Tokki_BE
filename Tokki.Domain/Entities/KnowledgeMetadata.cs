using System;
using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class KnowledgeMetadata
    {
        [Key]
        public string Id { get; set; } 

        [Required]
        public string TargetId { get; set; } 

        [Required]
        public KnowledgeType Type { get; set; }

        [Required]
        public string SearchTags { get; set; } 

        [Required]
        public string DescriptionForAi { get; set; } 

        public CurrentTopikLevel Level { get; set; }
    }
}