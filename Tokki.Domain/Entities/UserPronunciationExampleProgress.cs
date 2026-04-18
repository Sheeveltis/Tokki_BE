using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    [Table("UserPronunciationExampleProgress")]
    public class UserPronunciationExampleProgress
    {
        [Key]
        [Column(TypeName = "varchar(15)")]
        public string UserExampleProgressId { get; set; } = null!;

        [Required]
        [Column(TypeName = "nvarchar(15)")]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(10)")]
        public string PronunciationExampleId { get; set; } = null!;

        [ForeignKey(nameof(PronunciationExampleId))]
        public virtual PronunciationExample PronunciationExample { get; set; } = null!;

        public bool IsPracticed { get; set; } = false;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }
}
