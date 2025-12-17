using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyExampleResponse
    {
        public string ExampleId { get; set; } = string.Empty;
        public string Sentence { get; set; } = string.Empty;
        public string? Translation { get; set; }
    }
}
