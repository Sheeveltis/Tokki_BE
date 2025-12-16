using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class AddVocabulariesToTopicResponse
    {
        public string TopicId { get; set; } = string.Empty;
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount => TotalRequested - SuccessCount;
        public List<VocabularyAdditionResult> Results { get; set; } = new();
    }
}
