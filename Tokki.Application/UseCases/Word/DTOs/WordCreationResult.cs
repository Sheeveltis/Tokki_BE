using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class WordCreationResult
    {
        public string Text { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? WordId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
