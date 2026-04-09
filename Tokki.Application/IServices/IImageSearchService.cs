using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IImageSearchService
    {
        Task<List<string>> SearchImagesAsync(string query, int count = 1);

        Task<List<string>> SearchImagesForVocabAsync(string viDefinition, string koText, int count = 1);

        /// <summary>
        /// Dùng Gemini AI sinh ảnh minh hoạ cho từ vựng.
        /// Trả về byte[] của ảnh PNG do AI tạo ra.
        /// </summary>
        Task<byte[]?> GenerateImageForVocabAsync(string viDefinition, string koText);
    }
}
