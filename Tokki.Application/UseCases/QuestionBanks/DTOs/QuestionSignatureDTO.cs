using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class QuestionSignatureDTO
    {
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public List<string> OptionContents { get; set; } = new();
    }
}
