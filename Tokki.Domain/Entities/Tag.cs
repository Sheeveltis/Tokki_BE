using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class Tag
    {
        [Key]
        [MaxLength(10)]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;

        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
