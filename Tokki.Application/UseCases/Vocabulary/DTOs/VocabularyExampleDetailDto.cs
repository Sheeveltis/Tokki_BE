using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class VocabularyExampleDetailDto
    {
        public string ExampleId { get; set; } = string.Empty;
        public string Sentence { get; set; } = string.Empty;
        public string? Translation { get; set; }

        public VocabularyExampleStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public string CreateBy { get; set; } = string.Empty;
    }
}
