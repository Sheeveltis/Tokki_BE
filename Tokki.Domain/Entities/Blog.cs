using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Blog
    {
        [Key]
        [MaxLength(10)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Slug { get; set; } = string.Empty; 

        public string? ThumbnailUrl { get; set; }

        public int ViewCount { get; set; } = 0;

        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        public BlogStatus Status { get; set; } = BlogStatus.Draft;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public string AuthorId { get; set; }


        [ForeignKey("Category")]
        public string CategoryId { get; set; } = string.Empty;
        public virtual Category? Category { get; set; }

        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
