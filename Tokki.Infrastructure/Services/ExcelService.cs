using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Net;
using System.Text;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Application.Services
{
    public class ExcelService : IExcelService
    {
        public ExcelService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
        }
        public Task<List<VocabularyExcelDTO>> ExtractVocabularyDataAsync(IFormFile file)
        {
            var result = new List<VocabularyExcelDTO>();

            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                    return Task.FromResult(result);

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var text = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var pronunciation = worksheet.Cells[row, 2].Text;
                    var imgUrl = worksheet.Cells[row, 3].Text;
                    var definition = worksheet.Cells[row, 4].Text;
                    result.Add(new VocabularyExcelDTO
                    {
                        Text = text.Trim(),
                        Pronunciation = pronunciation.Trim(),
                        ImageUrl = imgUrl.Trim(), 
                        Definition = definition.Trim()
                    });
                }
            }

            return Task.FromResult(result);
        }
        public Task<byte[]> ExportVocabularyToExcelAsync(List<VocabularyExportDTO> data, string sheetName)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                worksheet.Cells[1, 1].Value = "Text";
                worksheet.Cells[1, 2].Value = "Pronunciation";
                worksheet.Cells[1, 3].Value = "ImgURL";
                worksheet.Cells[1, 4].Value = "Definition";

                using (var range = worksheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2; 

                    worksheet.Cells[rowIndex, 1].Value = item.Text;
                    worksheet.Cells[rowIndex, 2].Value = item.Pronunciation;
                    worksheet.Cells[rowIndex, 3].Value = item.ImgURL ?? "(Không có ảnh)";
                    worksheet.Cells[rowIndex, 4].Value = item.Definition;
                }

                worksheet.Cells.AutoFitColumns();

                return Task.FromResult(package.GetAsByteArray());
            }
        }
        public async Task<QuestionBankImportDTO> ExtractQuestionBankDataAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File Excel không hợp lệ.");

            var result = new QuestionBankImportDTO();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count < 3)
                        throw new ArgumentException("File Excel thiếu sheet (Yêu cầu: Passages, Questions, Options).");

                    result.Passages = ReadPassages(package.Workbook.Worksheets[0]);
                    result.Questions = ReadQuestions(package.Workbook.Worksheets[1]);
                    result.Options = ReadOptions(package.Workbook.Worksheets[2]);
                }
            }
            return result;
        }


        private List<ExcelPassageDTO> ReadPassages(ExcelWorksheet sheet)
        {
            var list = new List<ExcelPassageDTO>();
            int rowCount = sheet.Dimension?.Rows ?? 0;
            for (int row = 2; row <= rowCount; row++)
            {
                var refId = GetCellValue(sheet, row, 1);
                if (string.IsNullOrEmpty(refId)) continue;

                list.Add(new ExcelPassageDTO
                {
                    RowIndex = row,
                    RefId = refId,
                    Title = GetCellValue(sheet, row, 2), 
                    Content = GetCellHtml(sheet.Cells[row, 3]),
                    ImageUrl = GetCellValue(sheet, row, 4),
                    MediaType = GetCellValue(sheet, row, 5),
                    Status = GetCellValue(sheet, row, 6)
                });
            }
            return list;
        }

        private List<ExcelQuestionDTO> ReadQuestions(ExcelWorksheet sheet)
        {
            var list = new List<ExcelQuestionDTO>();
            int rowCount = sheet.Dimension?.Rows ?? 0;
            for (int row = 2; row <= rowCount; row++)
            {
                var refId = GetCellValue(sheet, row, 1); 
                if (string.IsNullOrEmpty(refId)) continue;

                list.Add(new ExcelQuestionDTO
                {
                    RowIndex = row,
                    RefPassageId = GetCellValue(sheet, row, 2), 
                    RefId = refId,
                    Content = GetCellHtml(sheet.Cells[row, 3]),
                    Explanation = GetCellHtml(sheet.Cells[row, 4]), 
                    MediaUrl = GetCellValue(sheet, row, 5),     
                    MediaType = GetCellValue(sheet, row, 6),    
                    Status = GetCellValue(sheet, row, 7)
                });
            }
            return list;
        }

        private List<ExcelOptionDTO> ReadOptions(ExcelWorksheet sheet)
        {
            var list = new List<ExcelOptionDTO>();
            int rowCount = sheet.Dimension?.Rows ?? 0;
            for (int row = 2; row <= rowCount; row++)
            {
                var refId = GetCellValue(sheet, row, 1); // OptionId
                if (string.IsNullOrEmpty(refId)) continue;

                list.Add(new ExcelOptionDTO
                {
                    RowIndex = row,
                    RefId = refId,
                    RefQuestionId = GetCellValue(sheet, row, 2), // QuestionBankId
                    KeyOption = GetCellValue(sheet, row, 3),     // KeyOption (A, B, C...)
                    Content = GetCellHtml(sheet.Cells[row, 4]),  // Content
                    ImageUrl = GetCellValue(sheet, row, 5),      // [NEW] ImageUrl
                    IsCorrectStr = GetCellValue(sheet, row, 6)   // IsCorrect
                });
            }
            return list;
        }
        private string GetCellValue(ExcelWorksheet sheet, int row, int col)
        {
            var value = sheet.Cells[row, col].Text?.Trim();
            if (string.IsNullOrEmpty(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return value;
        }
        private string GetCellHtml(ExcelRange cell)
        {
            var rawValue = cell.Value?.ToString()?.Trim();

            if (string.IsNullOrEmpty(rawValue) || rawValue.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (cell.RichText.Count > 0)
            {
                var sb = new StringBuilder();

                foreach (var part in cell.RichText)
                {
                    var textPart = part.Text;
                    if (string.IsNullOrEmpty(textPart)) continue;

                    string encodedText = SanitizeHtml(textPart);

                    if (part.UnderLine) encodedText = $"<u>{encodedText}</u>";
                    if (part.Italic) encodedText = $"<i>{encodedText}</i>";
                    if (part.Bold) encodedText = $"<b>{encodedText}</b>";
                    if (part.Strike) encodedText = $"<del>{encodedText}</del>";

                    encodedText = encodedText.Replace("\r\n", "<br/>").Replace("\n", "<br/>");

                    sb.Append(encodedText);
                }
                return sb.ToString();
            }

            return SanitizeHtml(rawValue)
                     .Replace("\r\n", "<br/>")
                     .Replace("\n", "<br/>");
        }

        /// <summary>
        /// Hàm encode nhẹ: Chỉ xử lý ký tự HTML đặc biệt, giữ nguyên Tiếng Việt
        /// </summary>
        private string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input.Replace("&", "&amp;")  
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#39;");
        }

        public Task<List<PronunciationExampleExcelDTO>> ExtractExampleDataAsync(IFormFile file)
        {
            var result = new List<PronunciationExampleExcelDTO>();

            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                    return Task.FromResult(result);

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var ruleId = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(ruleId)) continue;

                    var targetScript = worksheet.Cells[row, 2].Text;

                    var rawScript = worksheet.Cells[row, 3].Text;

                    var phoneticScript = worksheet.Cells[row, 4].Text;

                    var meaning = worksheet.Cells[row, 5].Text;

                    int.TryParse(worksheet.Cells[row, 6].Text, out int sortOrder);

                    result.Add(new PronunciationExampleExcelDTO
                    {
                        PronunciationRuleId = ruleId.Trim(),
                        TargetScript = targetScript.Trim(),
                        RawScript = rawScript.Trim(),
                        PhoneticScript = phoneticScript.Trim(),
                        Meaning = meaning.Trim(),
                        SortOrder = sortOrder
                    });
                }
            }

            return Task.FromResult(result);
        }
    }
}