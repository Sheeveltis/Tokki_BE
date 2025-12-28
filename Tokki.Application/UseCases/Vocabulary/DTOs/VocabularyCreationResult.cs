using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyCreationResult
    {
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string AudioURL { get; set; }
        public string? VocabularyId { get; set; }
        public string? Message { get; set; }
        public List<Error>? Errors { get; set; }
    }
}
