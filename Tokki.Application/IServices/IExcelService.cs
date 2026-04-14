using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.IServices
{
    public interface IExcelService
    {
        Task<List<VocabularyExcelDTO>> ExtractVocabularyDataAsync(IFormFile file);
        Task<byte[]> ExportVocabularyToExcelAsync(List<VocabularyExportDTO> data, string sheetName);
        Task<QuestionBankImportDTO> ExtractQuestionBankDataAsync(IFormFile file);
        Task<List<PronunciationExampleExcelDTO>> ExtractExampleDataAsync(IFormFile file);
        Task<byte[]> ExportExamplesToExcelAsync(List<PronunciationExampleExcelDTO> data, string sheetName);
        Task<List<PronunciationRuleExcelDTO>> ExtractRuleDataAsync(IFormFile file);
        Task<byte[]> ExportRulesToExcelAsync(List<PronunciationRuleExcelDTO> data, string sheetName);
        Task<List<TitleExcelDTO>> ExtractTitleDataAsync(IFormFile file);
        Task<byte[]> ExportTitlesToExcelAsync(List<TitleExcelDTO> data, string sheetName);
<<<<<<< HEAD
        Task<byte[]> ExportVocabularyImageResultsToExcelAsync(List<VocabularyImageResultDto> results);
=======
        Task<List<CategoryExcelDTO>> ExtractCategoryDataAsync(IFormFile file);
        Task<byte[]> ExportCategoriesToExcelAsync(List<CategoryExcelDTO> data, string sheetName);
        Task<List<BlogExcelDTO>> ExtractBlogDataAsync(IFormFile file);
        Task<byte[]> ExportBlogsToExcelAsync(List<BlogExcelDTO> data, string sheetName);
        Task<List<SystemConfigExcelDTO>> ExtractSystemConfigDataAsync(IFormFile file);
        Task<byte[]> ExportSystemConfigsToExcelAsync(List<SystemConfigExcelDTO> data, string sheetName);
>>>>>>> 7073aade6fab71e0909e22ecfdf8d819e3351d33
    }
}
