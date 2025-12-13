using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class WordCreateDto
    {
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public List<MeaningCreateDto> Meanings { get; set; } = new();
    }
}
