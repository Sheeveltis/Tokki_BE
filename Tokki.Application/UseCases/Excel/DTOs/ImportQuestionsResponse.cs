using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class ImportQuestionsResponse
    {
        public List<ImportedQuestionSuccess> SuccessItems { get; set; } = new();
        public List<ImportedQuestionError> Errors { get; set; } = new();

        public int TotalSuccess => SuccessItems.Count;
        public int TotalFailed => Errors.Count;
    }

    public class ImportedQuestionSuccess
    {
        public int ExcelRowIndex { get; set; } // Dòng trong Excel
        public string ExcelRefId { get; set; } // ID tham chiếu (Q_001)
        public string RealId { get; set; }     // ID thật trong DB
        public string Content { get; set; } = string.Empty;

        public ImportedPassageInfo? LinkedPassage { get; set; }
        public List<ImportedOptionInfo> Options { get; set; } = new();
    }

    public class ImportedPassageInfo
    {
        public string Title { get; set; } = string.Empty;
        public string RealId { get; set; } = string.Empty;
    }

    public class ImportedOptionInfo
    {
        public string Key { get; set; } = string.Empty; // A, B, C
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class ImportedQuestionError
    {
        public int ExcelRowIndex { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public string ContentSummary { get; set; } = string.Empty;
        public string ErrorReason { get; set; } = string.Empty;
    }

    public class ExcelRawPassage
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string MediaType { get; set; }
        public string Status { get; set; }
    }

    public class ExcelRawQuestion
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string RefPassageId { get; set; }
        public string Content { get; set; }
        public string Explanation { get; set; }
        public string Status { get; set; }
    }

    public class ExcelRawOption
    {
        public int RowIndex { get; set; }
        public string RefId { get; set; }
        public string RefQuestionId { get; set; }
        public string KeyOption { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }
}
