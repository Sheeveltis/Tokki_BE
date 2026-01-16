using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class QuestionBankImportDTO
    {
        public List<ExcelPassageDTO> Passages { get; set; } = new();
        public List<ExcelQuestionDTO> Questions { get; set; } = new();
        public List<ExcelOptionDTO> Options { get; set; } = new();
    }

    public class ExcelPassageDTO
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string MediaType { get; set; }
        public string Status { get; set; }
    }

    public class ExcelQuestionDTO
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string RefPassageId { get; set; }
        public string Content { get; set; }
        public string Explanation { get; set; }
        public string Status { get; set; }
    }

    public class ExcelOptionDTO
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string RefQuestionId { get; set; }
        public string KeyOption { get; set; }
        public string Content { get; set; }
        public string IsCorrectStr { get; set; }
    }
}
