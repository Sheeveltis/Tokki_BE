using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class TopicDetailDto
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }

        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        // NEW: Người duyệt + thời gian duyệt
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public TopicLevel Level { get; set; }
        public string? ImgUrl { get; set; }
        public TopicStatus Status { get; set; }

        public int VocabularyCount { get; set; }
        public List<VocabularyDto> Vocabularies { get; set; } = new List<VocabularyDto>();
    }
}
