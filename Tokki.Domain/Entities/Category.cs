using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class Category
    {
        [Key]
        [MaxLength(10)] // NanoID
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; 

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Quan hệ: 1 Danh mục có nhiều Bài viết
        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
