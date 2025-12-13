using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class WordWithMeaningsDto
    {
        public string WordId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public List<WordMeaningDto> Meanings { get; set; } = new();
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public WordStatus Status { get; set; }
    }
}
