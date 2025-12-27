using System;
using System.Collections.Generic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    /// <summary>
    /// DTO trả về thông tin vocabulary đầy đủ
    /// </summary>
    public class VocabularyDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string? ImgURL { get; set; }
        
        /// <summary>
        /// Danh sách topics mà vocabulary này thuộc về
        /// </summary>
        public List<TopicInfoDto> Topics { get; set; } = new();

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public VocabularyStatus Status { get; set; }
    }
}
