using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationExample.DTOs
{
    public class ExampleSimpleDTO
    {
        public string ExampleId { get; set; } = string.Empty;
        public string RawScript { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
