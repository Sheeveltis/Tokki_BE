using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.ExcelCore
{
    public class ExcelImportResult<T>
    {
        public List<ExcelSuccessDetail<T>> SuccessItems { get; set; } = new();
        public List<ExcelErrorDetail> Errors { get; set; } = new();

        public int TotalSuccess => SuccessItems.Count;
        public int TotalFailed => Errors.Count;
    }

    public class ExcelSuccessDetail<T>
    {
        public int RowIndex { get; set; }
        public T Data { get; set; } = default!;
    }

    public class ExcelErrorDetail
    {
        public int RowIndex { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public string ContentSummary { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
