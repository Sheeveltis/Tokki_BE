using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.ExcelCore
{
    public interface IExcelBaseService
    {
        Task<byte[]> GenerateTemplateAsync<T>(string sheetName = "Template");

        Task<byte[]> ExportAsync<T>(
            IEnumerable<T> data,
            string sheetName = "Data",
            List<string>? ignoredColumns = null);

        Task<ExcelImportResult<T>> ImportAsync<T>(
            IFormFile file,
            string? sheetName = null,
            CancellationToken cancellationToken = default) where T : new();
    }
}
