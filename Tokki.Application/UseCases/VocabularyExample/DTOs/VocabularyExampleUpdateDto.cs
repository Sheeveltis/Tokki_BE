using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabularyExample.DTOs
{
    public class VocabularyExampleUpdateDto
    {
        // Cho phép update từng phần
        public string? Sentence { get; set; }
        public string? Translation { get; set; }
        public VocabularyExampleStatus? Status { get; set; }
    }
}
