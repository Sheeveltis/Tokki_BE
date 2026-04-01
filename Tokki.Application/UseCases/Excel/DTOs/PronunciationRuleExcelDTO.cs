using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class PronunciationRuleExcelDTO
    {
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Content { get; set; }
        public int SortOrder { get; set; }
    }
}
