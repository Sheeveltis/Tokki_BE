namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyImageResultDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? OriginalImgURL { get; set; }
        public string? NewImgURL { get; set; }          // Ảnh được chọn để lưu vào DB
        public string? ViImgURL { get; set; }           // Ảnh tìm bằng tiếng Việt
        public string? KoImgURL { get; set; }           // Ảnh tìm bằng tiếng Hàn
        public string Status { get; set; } = string.Empty; // "Success", "Failed", "Skipped"
        public string? ErrorMessage { get; set; }
    }

    public class AutoFindImagesResultDto
    {
        public List<VocabularyImageResultDto> Results { get; set; } = new();
        public int TotalCount => Results.Count;
        public int SuccessCount => Results.Count(r => r.Status == "Success");
        public int FailedCount => Results.Count(r => r.Status == "Failed");
        public int SkippedCount => Results.Count(r => r.Status == "Skipped");
    }
}
