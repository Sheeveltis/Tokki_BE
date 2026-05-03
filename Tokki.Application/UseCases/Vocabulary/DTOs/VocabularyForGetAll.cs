using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyForGetAll
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public string Definition { get; set; } = string.Empty;
        public int? LevelTopic { get; set; }
        public string? LevelLabel { get; set; } // Thêm Label cho dynamic

        /// <summary>
        /// Danh sách topics mà vocabulary này thuộc về
        /// </summary>

        public VocabularyStatus Status { get; set; }
    }
}
