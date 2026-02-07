using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class QuestionOptionDto
    {
        public string OptionId { get; set; } = string.Empty;
        public string KeyOption { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCorrect { get; set; }
    }
}
