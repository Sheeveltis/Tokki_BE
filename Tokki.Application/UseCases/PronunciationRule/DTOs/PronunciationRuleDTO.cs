using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationRule.DTOs
{
    public class PronunciationRuleDTO
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsLearned { get; set; } // Trạng thái đã học xong cả bài
        public int TotalExamples { get; set; } // Tổng số ví dụ
        public int PracticedCount { get; set; } // Số ví dụ đã luyện tập
        public int ProgressPercent { get; set; } // % tiến độ
        public string ProgressDisplay => $"{PracticedCount}/{TotalExamples}"; // Hiển thị dạng "1/13"
    }
}
