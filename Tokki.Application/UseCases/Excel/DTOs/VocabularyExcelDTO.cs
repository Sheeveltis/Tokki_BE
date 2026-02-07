namespace Tokki.Application.UseCases.Excel.DTOs
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
        public List<VocabularyPreviewDTO> AddedNewVocabList { get; set; } = new();

        public List<VocabularyPreviewDTO> LinkedExistingVocabList { get; set; } = new();

        public List<VocabularyPreviewDTO> FailureList { get; set; } = new();
        public int AddedNewCount => AddedNewVocabList.Count;
        public int LinkedExistingCount => LinkedExistingVocabList.Count;
        public int FailureCount => FailureList.Count;
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