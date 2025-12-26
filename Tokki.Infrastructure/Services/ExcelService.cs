using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.DTOs;

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
    }
}