using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class WordMeaningsResponse
    {
        public string WordId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public List<MeaningDto> Meanings { get; set; } = new();
    }
}
