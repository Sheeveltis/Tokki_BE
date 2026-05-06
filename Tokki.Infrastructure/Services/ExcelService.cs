using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Application.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ExcelService(IHttpClientFactory httpClientFactory)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
            _httpClientFactory = httpClientFactory;
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
                    var text = GetCellValue(worksheet, row, 1);
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var pronunciation = GetCellValue(worksheet, row, 2);
                    var imgUrl = GetCellValue(worksheet, row, 3);
                    var definition = GetCellValue(worksheet, row, 4);

                    result.Add(new VocabularyExcelDTO
                    {
                        Text = text,
                        Pronunciation = pronunciation ?? string.Empty,
                        ImageUrl = imgUrl ?? string.Empty, 
                        Definition = definition ?? string.Empty
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
                    var targetScript = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(targetScript)) continue;

                    var rawScript = worksheet.Cells[row, 2].Text;
                    var phoneticScript = worksheet.Cells[row, 3].Text;
                    var meaning = worksheet.Cells[row, 4].Text;
                    int.TryParse(worksheet.Cells[row, 5].Text, out int sortOrder);
                    var difficulty = worksheet.Cells[row, 6].Text;

                    result.Add(new PronunciationExampleExcelDTO
                    {
                        TargetScript = targetScript.Trim(),
                        RawScript = rawScript.Trim(),
                        PhoneticScript = phoneticScript.Trim(),
                        Meaning = meaning.Trim(),
                        SortOrder = sortOrder,
                        Difficulty = difficulty?.Trim()
                    });
                }
            }

            return Task.FromResult(result);
        }

        public Task<byte[]> ExportExamplesToExcelAsync(List<PronunciationExampleExcelDTO> data, string sheetName)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                worksheet.Cells[1, 1].Value = "TargetScript";
                worksheet.Cells[1, 2].Value = "RawScript";
                worksheet.Cells[1, 3].Value = "PhoneticScript";
                worksheet.Cells[1, 4].Value = "Meaning";
                worksheet.Cells[1, 5].Value = "SortOrder";
                worksheet.Cells[1, 6].Value = "Difficulty";

                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2;

                    worksheet.Cells[rowIndex, 1].Value = item.TargetScript;
                    worksheet.Cells[rowIndex, 2].Value = item.RawScript;
                    worksheet.Cells[rowIndex, 3].Value = item.PhoneticScript;
                    worksheet.Cells[rowIndex, 4].Value = item.Meaning;
                    worksheet.Cells[rowIndex, 5].Value = item.SortOrder;
                    worksheet.Cells[rowIndex, 6].Value = item.Difficulty;
                }

                worksheet.Cells.AutoFitColumns();

                return Task.FromResult(package.GetAsByteArray());
            }
        }

        public Task<List<PronunciationRuleExcelDTO>> ExtractRuleDataAsync(IFormFile file)
        {
            var result = new List<PronunciationRuleExcelDTO>();

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
                    var ruleName = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(ruleName)) continue;

                    var description = worksheet.Cells[row, 2].Text;
                    var content = worksheet.Cells[row, 3].Text;
                    int.TryParse(worksheet.Cells[row, 4].Text, out int sortOrder);

                    result.Add(new PronunciationRuleExcelDTO
                    {
                        RuleName = ruleName.Trim(),
                        Description = description?.Trim(),
                        Content = content?.Trim(),
                        SortOrder = sortOrder
                    });
                }
            }

            return Task.FromResult(result);
        }

        public Task<byte[]> ExportRulesToExcelAsync(List<PronunciationRuleExcelDTO> data, string sheetName)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                worksheet.Cells[1, 1].Value = "RuleName";
                worksheet.Cells[1, 2].Value = "Description";
                worksheet.Cells[1, 3].Value = "Content";
                worksheet.Cells[1, 4].Value = "SortOrder";

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

                    worksheet.Cells[rowIndex, 1].Value = item.RuleName;
                    worksheet.Cells[rowIndex, 2].Value = item.Description;
                    worksheet.Cells[rowIndex, 3].Value = item.Content;
                    worksheet.Cells[rowIndex, 4].Value = item.SortOrder;
                }

                worksheet.Cells.AutoFitColumns();

                return Task.FromResult(package.GetAsByteArray());
            }
        }
        public async Task<List<TitleExcelDTO>> ExtractTitleDataAsync(IFormFile file)
        {
            var result = new List<TitleExcelDTO>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var name = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        result.Add(new TitleExcelDTO
                        {
                            Name = name.Trim(),
                            Description = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            ColorHex = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "#000000",
                            IconUrl = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                            RequirementType = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? "Level",
                            RequirementQuantity = long.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out long q) ? q : 0,
                            Status = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "Active"
                        });
                    }
                }
            }
            return result;
        }

        public async Task<byte[]> ExportTitlesToExcelAsync(List<TitleExcelDTO> data, string sheetName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells[1, 1].Value = "Tên danh hiệu";
                worksheet.Cells[1, 2].Value = "Mô tả";
                worksheet.Cells[1, 3].Value = "Mã màu (HEX)";
                worksheet.Cells[1, 4].Value = "URL Icon";
                worksheet.Cells[1, 5].Value = "Loại điều kiện";
                worksheet.Cells[1, 6].Value = "Giá trị điều kiện";
                worksheet.Cells[1, 7].Value = "Trạng thái (Active/Inactive)";

                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2;
                    worksheet.Cells[rowIndex, 1].Value = item.Name;
                    worksheet.Cells[rowIndex, 2].Value = item.Description;
                    worksheet.Cells[rowIndex, 3].Value = item.ColorHex;
                    worksheet.Cells[rowIndex, 4].Value = item.IconUrl;
                    worksheet.Cells[rowIndex, 5].Value = item.RequirementType;
                    worksheet.Cells[rowIndex, 6].Value = item.RequirementQuantity;
                    worksheet.Cells[rowIndex, 7].Value = item.Status;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> ExportVocabularyImageResultsToExcelAsync(List<VocabularyImageResultDto> results)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Kết quả tìm ảnh");

            const int imgHeight = 80;
            const int imgWidth  = 80;
            const double rowHeightPt = 62;

            var headers = new[] { "STT", "Text (Tiếng Hàn)", "Definition (Nghĩa)",
                                   "🎨 Ảnh AI (Gemini)", "🔍 Ảnh Pixabay", "Trạng thái", "Ghi chú" };
            for (int col = 0; col < headers.Length; col++)
                ws.Cells[1, col + 1].Value = headers[col];

            using (var range = ws.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment   = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            }

            ws.Column(4).Width = 14;
            ws.Column(5).Width = 14;

            var httpClient = _httpClientFactory.CreateClient();

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                int row = i + 2;

                ws.Row(row).Height = rowHeightPt;

                ws.Cells[row, 1].Value = i + 1;
                ws.Cells[row, 2].Value = item.Text;
                ws.Cells[row, 3].Value = item.Definition;
                ws.Cells[row, 6].Value = item.Status;
                ws.Cells[row, 7].Value = item.ErrorMessage ?? "";

                Color rowColor = item.Status switch
                {
                    "Success" => Color.FromArgb(198, 239, 206),
                    "Failed"  => Color.FromArgb(255, 199, 206),
                    "Skipped" => Color.FromArgb(255, 235, 156),
                    _         => Color.White
                };
                using (var rowRange = ws.Cells[row, 1, row, headers.Length])
                {
                    rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    rowRange.Style.Fill.BackgroundColor.SetColor(rowColor);
                    rowRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                await TryEmbedImageAsync(ws, httpClient, item.ViImgURL, row, 4, imgWidth, imgHeight);
                await TryEmbedImageAsync(ws, httpClient, item.KoImgURL, row, 5, imgWidth, imgHeight);
            }

            ws.Column(1).AutoFit();
            ws.Column(2).AutoFit();
            ws.Column(3).AutoFit();
            ws.Column(6).AutoFit();
            ws.Column(7).AutoFit();

            var summaryWs = package.Workbook.Worksheets.Add("Tổng kết");
            summaryWs.Cells[1, 1].Value = "Thống kê";
            summaryWs.Cells[1, 1].Style.Font.Bold = true;
            summaryWs.Cells[1, 1].Style.Font.Size = 14;
            summaryWs.Cells[3, 1].Value = "Tổng số từ vựng:";
            summaryWs.Cells[3, 2].Value = results.Count;
            summaryWs.Cells[4, 1].Value = "Thành công:";
            summaryWs.Cells[4, 2].Value = results.Count(r => r.Status == "Success");
            summaryWs.Cells[4, 2].Style.Font.Color.SetColor(Color.Green);
            summaryWs.Cells[5, 1].Value = "Thất bại:";
            summaryWs.Cells[5, 2].Value = results.Count(r => r.Status == "Failed");
            summaryWs.Cells[5, 2].Style.Font.Color.SetColor(Color.Red);
            summaryWs.Cells[6, 1].Value = "Bỏ qua:";
            summaryWs.Cells[6, 2].Value = results.Count(r => r.Status == "Skipped");
            summaryWs.Cells[6, 2].Style.Font.Color.SetColor(Color.Orange);
            summaryWs.Cells[7, 1].Value = "Có ảnh EN:";
            summaryWs.Cells[7, 2].Value = results.Count(r => !string.IsNullOrEmpty(r.ViImgURL));
            summaryWs.Cells[8, 1].Value = "Có ảnh KO:";
            summaryWs.Cells[8, 2].Value = results.Count(r => !string.IsNullOrEmpty(r.KoImgURL));
            summaryWs.Cells[9, 1].Value = "Thời gian:";
            summaryWs.Cells[9, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            summaryWs.Cells[3, 1, 9, 1].Style.Font.Bold = true;
            summaryWs.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        private async Task TryEmbedImageAsync(
            ExcelWorksheet ws, HttpClient client,
            string? imageUrl, int row, int col,
            int imgWidthPx, int imgHeightPx)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var bytes = await client.GetByteArrayAsync(imageUrl);
                var tempFile = Path.Combine(Path.GetTempPath(), $"tokki_img_{row}_{col}_{Guid.NewGuid():N}.jpg");
                await File.WriteAllBytesAsync(tempFile, bytes);

                var picName = $"img_r{row}_c{col}";
                var picture = ws.Drawings.AddPicture(picName, new FileInfo(tempFile));

                picture.SetPosition(row - 1, 2, col - 1, 2);
                picture.SetSize(imgWidthPx, imgHeightPx);

                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [Excel] Không nhúng được ảnh row={row} col={col}: {ex.Message}");
            }
        }
        public async Task<List<CategoryExcelDTO>> ExtractCategoryDataAsync(IFormFile file)
        {
            var result = new List<CategoryExcelDTO>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var name = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        result.Add(new CategoryExcelDTO
                        {
                            Name = name.Trim(),
                            Slug = worksheet.Cells[row, 2].Value?.ToString()?.Trim()
                        });
                    }
                }
            }
            return result;
        }

        public async Task<byte[]> ExportCategoriesToExcelAsync(List<CategoryExcelDTO> data, string sheetName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells[1, 1].Value = "Tên danh mục";
                worksheet.Cells[1, 2].Value = "Slug (Tùy chọn)";

                using (var range = worksheet.Cells[1, 1, 1, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2;
                    worksheet.Cells[rowIndex, 1].Value = item.Name;
                    worksheet.Cells[rowIndex, 2].Value = item.Slug;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        public async Task<List<BlogExcelDTO>> ExtractBlogDataAsync(IFormFile file)
        {
            var result = new List<BlogExcelDTO>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var title = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(title)) continue;

                        result.Add(new BlogExcelDTO
                        {
                            Title = title.Trim(),
                            ThumbnailUrl = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            ShortDescription = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? string.Empty,
                            Content = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? string.Empty,
                            CategoryName = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? string.Empty,
                            Tags = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? string.Empty,
                            Slug = worksheet.Cells[row, 7].Value?.ToString()?.Trim()
                        });
                    }
                }
            }
            return result;
        }

        public async Task<byte[]> ExportBlogsToExcelAsync(List<BlogExcelDTO> data, string sheetName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells[1, 1].Value = "Tiêu đề";
                worksheet.Cells[1, 2].Value = "URL Thumbnail";
                worksheet.Cells[1, 3].Value = "Mô tả ngắn";
                worksheet.Cells[1, 4].Value = "Nội dung";
                worksheet.Cells[1, 5].Value = "Tên danh mục";
                worksheet.Cells[1, 6].Value = "Tags (phân cách bằng dấu phẩy)";
                worksheet.Cells[1, 7].Value = "Slug";

                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2;
                    worksheet.Cells[rowIndex, 1].Value = item.Title;
                    worksheet.Cells[rowIndex, 2].Value = item.ThumbnailUrl;
                    worksheet.Cells[rowIndex, 3].Value = item.ShortDescription;
                    worksheet.Cells[rowIndex, 4].Value = item.Content;
                    worksheet.Cells[rowIndex, 5].Value = item.CategoryName;
                    worksheet.Cells[rowIndex, 6].Value = item.Tags;
                    worksheet.Cells[rowIndex, 7].Value = item.Slug;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
 
        public async Task<List<SystemConfigExcelDTO>> ExtractSystemConfigDataAsync(IFormFile file)
        {
            var result = new List<SystemConfigExcelDTO>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
  
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var key = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(key)) continue;
  
                        result.Add(new SystemConfigExcelDTO
                        {
                            Key = key.Trim(),
                            Value = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            Description = worksheet.Cells[row, 3].Value?.ToString()?.Trim(),
                            DataType = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                            ConfigType = worksheet.Cells[row, 5].Value?.ToString()?.Trim()
                        });
                    }
                }
            }
            return result;
        }
  
        public async Task<byte[]> ExportSystemConfigsToExcelAsync(List<SystemConfigExcelDTO> data, string sheetName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells[1, 1].Value = "Key";
                worksheet.Cells[1, 2].Value = "Value";
                worksheet.Cells[1, 3].Value = "Description";
                worksheet.Cells[1, 4].Value = "DataType";
                worksheet.Cells[1, 5].Value = "ConfigType";
  
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
  
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int rowIndex = i + 2;
                    worksheet.Cells[rowIndex, 1].Value = item.Key;
                    worksheet.Cells[rowIndex, 2].Value = item.Value;
                    worksheet.Cells[rowIndex, 3].Value = item.Description;
                    worksheet.Cells[rowIndex, 4].Value = item.DataType;
                    worksheet.Cells[rowIndex, 5].Value = item.ConfigType;
                }
  
                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        public Task<byte[]> GetPronunciationExampleTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("PronunciationExamples");
                string[] headers = { "TargetScript", "RawScript", "PhoneticScript", "Meaning", "SortOrder", "Difficulty" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                worksheet.Cells[2, 1].Value = "안녕하세요";
                worksheet.Cells[2, 2].Value = "안녕하세요";
                worksheet.Cells[2, 3].Value = "an-nyeong-ha-se-yo";
                worksheet.Cells[2, 4].Value = "Xin chào";
                worksheet.Cells[2, 5].Value = 1;
                worksheet.Cells[2, 6].Value = "Medium";
                worksheet.Cells.AutoFitColumns();
                return Task.FromResult(package.GetAsByteArray());
            }
        }

        public Task<byte[]> GetPronunciationRuleTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("TokkiProject");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("PronunciationRules");
                string[] headers = { "RuleName", "Description", "Content", "SortOrder" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                worksheet.Cells[2, 1].Value = "Quy tắc Patchim";
                worksheet.Cells[2, 2].Value = "Mô tả về cách phát âm phụ âm cuối";
                worksheet.Cells[2, 3].Value = "[{\"original\":\"a\",\"replacement\":\"b\"}]";
                worksheet.Cells[2, 4].Value = 1;
                worksheet.Cells.AutoFitColumns();
                return Task.FromResult(package.GetAsByteArray());
            }
        }

        public async Task<List<AlphabetExcelDTO>> ExtractAlphabetDataAsync(IFormFile file)
        {
            var result = new List<AlphabetExcelDTO>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var letter = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(letter)) continue;

                        result.Add(new AlphabetExcelDTO
                        {
                            Letter = letter.Trim(),
                            Meaning = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            Pronunciation = worksheet.Cells[row, 3].Value?.ToString()?.Trim(),
                            Type = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                            AudioUrl = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                            DisplayDataJson = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                            ValidationDataJson = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                            TotalStrokes = int.TryParse(worksheet.Cells[row, 8].Value?.ToString(), out int ts) ? ts : 0,
                            SortOrder = int.TryParse(worksheet.Cells[row, 9].Value?.ToString(), out int so) ? so : 0
                        });
                    }
                }
            }
            return result;
        }
    }
}