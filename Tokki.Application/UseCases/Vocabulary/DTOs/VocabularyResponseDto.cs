namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyResponseDto
    {
        public string VocabularyId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
    }
}
