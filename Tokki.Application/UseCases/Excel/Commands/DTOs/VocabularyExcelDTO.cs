using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.Commands.DTOs
{
    public class VocabularyExcelDTO
    {
        public string Text { get; set; }
        public string Pronunciation { get; set; }
        public string ImageUrl { get; set; } 
        public string Definition { get; set; }
    }
    public class ImportVocabularyResponse
    {
        public List<VocabularyPreviewDTO> SuccessList { get; set; } = new();
        public List<VocabularyPreviewDTO> FailureList { get; set; } = new();
    }

    public class VocabularyPreviewDTO
    {
        public string Text { get; set; }
        public string Definition { get; set; }
        public string Pronunciation { get; set; }
        public string? ImageUrl { get; set; }
        public string Reason { get; set; }
    }
}
