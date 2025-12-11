using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class TopicDto
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsActive { get; set; }
        public int VocabularyCount { get; set; }
    }
}
