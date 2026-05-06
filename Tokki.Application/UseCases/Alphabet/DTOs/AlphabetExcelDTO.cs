namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class AlphabetExcelDTO
    {
        public string? Letter { get; set; }
        public string? Meaning { get; set; }
        public string? Pronunciation { get; set; }
        public string? Type { get; set; } // "Vowel" or "Consonant" or "1" or "2"
        public string? AudioUrl { get; set; }
        public string? DisplayDataJson { get; set; }
        public string? ValidationDataJson { get; set; }
        public int TotalStrokes { get; set; }
        public int SortOrder { get; set; }
    }

    public class AlphabetImportResponse
    {
        public List<AlphabetPreviewDTO> SuccessList { get; set; } = new List<AlphabetPreviewDTO>();
        public List<AlphabetPreviewDTO> UpdateList { get; set; } = new List<AlphabetPreviewDTO>();
        public List<AlphabetPreviewDTO> FailureList { get; set; } = new List<AlphabetPreviewDTO>();
    }

    public class AlphabetPreviewDTO
    {
        public string? Letter { get; set; }
        public string? Type { get; set; }
        public string? Reason { get; set; }
    }
}
