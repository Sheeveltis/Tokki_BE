using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.VocabularyExample.DTOs
{
    public class AddVocabularyExamplesResponse
    {
        public string VocabularyId { get; set; } = string.Empty;
        public List<VocabularyExampleResponse> CreatedExamples { get; set; } = new();
        public List<string> SkippedSentences { get; set; } = new();
    }
}
