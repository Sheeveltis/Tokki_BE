using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.IServices
{
    public interface IExcelService
    {
        Task<List<VocabularyExcelDTO>> ExtractVocabularyDataAsync(IFormFile file);
        Task<byte[]> ExportVocabularyToExcelAsync(List<VocabularyExportDTO> data, string sheetName);
        Task<QuestionBankImportDTO> ExtractQuestionBankDataAsync(IFormFile file);
        Task<List<PronunciationExampleExcelDTO>> ExtractExampleDataAsync(IFormFile file);
    }
}
