using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class TopicDto
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TopicLevel Level { get; set; }
        public string? ImgUrl { get; set; }
        public int VocabularyCount { get; set; }
    }
}
