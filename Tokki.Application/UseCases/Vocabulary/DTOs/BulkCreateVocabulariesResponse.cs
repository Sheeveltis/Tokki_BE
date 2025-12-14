using System.Collections.Generic;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class BulkCreateVocabulariesResponse
    {
        public int TotalVocabularies { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<VocabularyCreationResult> Results { get; set; } = new();
    }
}
