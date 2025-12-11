using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.StatisticBlog.DTOs
{
    public class TopAuthorDTO
    {
        public string AuthorId { get; set; } = string.Empty;
        public int BlogCount { get; set; }
        public long TotalViews { get; set; }
    }
}
