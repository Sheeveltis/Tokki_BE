using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Application.Services
{
    public class ExcelService : IExcelService
    {
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
    }
}