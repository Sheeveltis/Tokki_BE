using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class Comment
    {
        [Key]
        [MaxLength(15)] 
        public string CommentId { get; set; } = string.Empty;
        [Required]
        [MaxLength(1000)] 
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false; 

        [Required]
        public string BlogId { get; set; } = string.Empty;

        [ForeignKey("BlogId")]
        public virtual Blog Blog { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!; 

        public string? ParentId { get; set; } 

        [ForeignKey("ParentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}