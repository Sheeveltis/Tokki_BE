using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Concurrent;
using System.Reflection;
using Tokki.Application.Common.ExcelCore;

namespace Tokki.Infrastructure.Services
{
    public class ExcelBaseService : IExcelBaseService
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyMapInfo>> _mappingCache = new();

        public ExcelBaseService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
        }

        private List<PropertyMapInfo> GetPropertyMappings<T>()
        {
            return _mappingCache.GetOrAdd(typeof(T), type =>
            {
                return type.GetProperties()
                    .Select(p => new
                    {
                        Property = p,
                        Attr = (ExcelColumnAttribute)Attribute.GetCustomAttribute(p, typeof(ExcelColumnAttribute))
                    })
                    .Where(x => x.Attr != null)
                    .Select(x => new PropertyMapInfo
                    {
                        Property = x.Property,
                        ColumnName = x.Attr.ColumnName,
                        Order = x.Attr.Order
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
            });
        }

        public async Task<byte[]> GenerateTemplateAsync<T>(string sheetName = "Template")
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);
            var mappings = GetPropertyMappings<T>();

            for (int i = 0; i < mappings.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = mappings[i].ColumnName;
            }

            FormatHeader(worksheet, mappings.Count);
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> ExportAsync<T>(
            IEnumerable<T> data,
            string sheetName = "Data",
            List<string>? ignoredColumns = null)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            var allMappings = GetPropertyMappings<T>();
            var mappings = allMappings
                .Where(m => ignoredColumns == null || !ignoredColumns.Contains(m.Property.Name))
                .ToList();

            if (!mappings.Any()) return await package.GetAsByteArrayAsync();

            int colCount = mappings.Count;

            for (int i = 0; i < colCount; i++)
            {
                worksheet.Cells[1, i + 1].Value = mappings[i].ColumnName;
            }
            FormatHeader(worksheet, colCount);

            int row = 2;
            if (data != null)
            {
                foreach (var item in data)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        var prop = mappings[col].Property;
                        var value = prop.GetValue(item);
                        var cell = worksheet.Cells[row, col + 1];

                        Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        if (value == null) cell.Value = string.Empty;
                        else if (propType == typeof(DateTime))
                        {
                            cell.Value = value;
                            cell.Style.Numberformat.Format = "dd/MM/yyyy";
                        }
                        else cell.Value = value;
                    }
                    row++;
                }
            }

            if (row > 1)
            {
                using var range = worksheet.Cells[1, 1, row - 1, colCount];
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.AutoFitColumns();
            }

            return await package.GetAsByteArrayAsync();
        }

        public async Task<ExcelImportResult<T>> ImportAsync<T>(
            IFormFile file,
            string? sheetName = null,
            CancellationToken cancellationToken = default) where T : new()
        {
            var result = new ExcelImportResult<T>();
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = string.IsNullOrEmpty(sheetName)
                ? package.Workbook.Worksheets.FirstOrDefault()
                : package.Workbook.Worksheets.FirstOrDefault(x => x.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (worksheet?.Dimension == null) return result;

            var mappings = GetPropertyMappings<T>();
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[1, col].Text?.Trim();
                if (!string.IsNullOrEmpty(header)) headerMap[header] = col;
            }

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (IsRowEmpty(worksheet, row, worksheet.Dimension.Columns)) continue;

                var item = new T();
                bool hasParseError = false;

                foreach (var map in mappings)
                {
                    if (!headerMap.TryGetValue(map.ColumnName, out int colIdx)) continue;

                    var cell = worksheet.Cells[row, colIdx];
                    try
                    {
                        SetPropertyValue(item, map.Property, cell.Value, cell.Text?.Trim());
                    }
                    catch (Exception)
                    {
                        result.Errors.Add(new ExcelErrorDetail
                        {
                            RowIndex = row,
                            SheetName = worksheet.Name,
                            Reason = $"Định dạng cột [{map.ColumnName}] không hợp lệ."
                        });
                        hasParseError = true;
                        break;
                    }
                }

                if (!hasParseError)
                {
                    result.SuccessItems.Add(new ExcelSuccessDetail<T>
                    {
                        RowIndex = row,
                        Data = item
                    });
                }
            }
            return result;
        }

        private void FormatHeader(ExcelWorksheet worksheet, int colCount)
        {
            if (colCount <= 0) return;
            using var range = worksheet.Cells[1, 1, 1, colCount];
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSkyBlue);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        private bool IsRowEmpty(ExcelWorksheet sheet, int row, int cols)
        {
            for (int i = 1; i <= cols; i++)
                if (!string.IsNullOrWhiteSpace(sheet.Cells[row, i].Text)) return false;
            return true;
        }

        private void SetPropertyValue<T>(T item, PropertyInfo prop, object val, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Type t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (t == typeof(string)) prop.SetValue(item, text);
            else if (t == typeof(DateTime)) prop.SetValue(item, val is double d ? DateTime.FromOADate(d) : DateTime.Parse(text));
            else if (t == typeof(bool)) prop.SetValue(item, text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1");
            else if (t.IsEnum) prop.SetValue(item, Enum.Parse(t, text, true));
            else prop.SetValue(item, Convert.ChangeType(text, t));
        }

        private class PropertyMapInfo
        {
            public PropertyInfo Property { get; set; } = null!;
            public string ColumnName { get; set; } = null!;
            public int Order { get; set; }
        }
    }
}