using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.FavoriteVocabulary.DTOs
{
    public class FavoriteVocabularyDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? ImgURL { get; set; }
        public string? AudioURL { get; set; }
        public DateTime FavoritedAt { get; set; }
    }
}
