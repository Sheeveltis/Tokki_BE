using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.DTOs
{
    public class ReviewItemDTO
    {
        public string UserVocabProgressId { get; set; }
        public string VocabularyId { get; set; }

        public BoxLevel BoxLevel { get; set; }
        public DateTime NextReviewAt { get; set; }
        public int Streak { get; set; }

        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
    }
}
