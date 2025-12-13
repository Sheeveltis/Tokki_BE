using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class MeaningCreateDto
    {
        public string Definition { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public string? ImgURL { get; set; }
    }
}
