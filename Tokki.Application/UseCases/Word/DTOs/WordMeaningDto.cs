using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class WordMeaningDto
    {
        public string MeaningId { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public string? ImgURL { get; set; }
        public MeaningStatus Status { get; set; }
    }
}
