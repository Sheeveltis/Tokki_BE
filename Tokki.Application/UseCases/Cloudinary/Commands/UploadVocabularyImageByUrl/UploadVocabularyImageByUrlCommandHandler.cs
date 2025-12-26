// Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImageByUrl/UploadVocabularyImageByUrlCommandHandler.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using System.Net.Http;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImageByUrl
{
    public class UploadVocabularyImageByUrlCommandHandler : IRequestHandler<UploadVocabularyImageByUrlCommand, OperationResult<string>>
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpClientFactory _httpClientFactory;

        public UploadVocabularyImageByUrlCommandHandler(ICloudinaryService cloudinaryService, IHttpClientFactory httpClientFactory)
        {
            _cloudinaryService = cloudinaryService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OperationResult<string>> Handle(UploadVocabularyImageByUrlCommand request, CancellationToken cancellationToken)
        {
            const string folderName = "tokki/vocab-image";

            // 1. Lấy link gốc (bỏ qua cdn-cgi)
            string cleanUrl = ExtractRealUrl(request.ImageUrl);

            try
            {
                // CÁCH 1: Thử để Cloudinary tự tải (Ưu tiên vì nhanh)
                var url = await _cloudinaryService.UploadImageFromUrlAsync(cleanUrl, folderName);
                if (!string.IsNullOrEmpty(url)) return OperationResult<string>.Success(url);
                throw new Exception("Cloudinary trả về null");
            }
            catch
            {
                // CÁCH 2: FALLBACK - Server tự tải về rồi Upload lên
                try
                {
                    return await DownloadAndUploadManuallyAsync(cleanUrl, folderName);
                }
                catch (Exception exManual)
                {
                    return OperationResult<string>.Failure($"Upload thất bại (kể cả fallback): {exManual.Message}", 500);
                }
            }
        }

        private async Task<OperationResult<string>> DownloadAndUploadManuallyAsync(string url, string folderName)
        {
            using var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            client.DefaultRequestHeaders.Add("Referer", "https://quizlet.com/");

            client.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");

            try
            {
                // Tải ảnh về RAM
                var imageBytes = await client.GetByteArrayAsync(url);

                var fileName = $"img_fallback_{Guid.NewGuid()}.jpg";

                // Upload lên Cloudinary
                var cloudUrl = await _cloudinaryService.UploadImageFromBytesAsync(imageBytes, fileName, folderName);

                return OperationResult<string>.Success(cloudUrl);
            }
            catch (HttpRequestException httpEx)
            {
                // Log rõ lỗi HTTP nếu vẫn bị chặn
                return OperationResult<string>.Failure($"HTTP Error khi tải ảnh từ nguồn: {httpEx.StatusCode} - {httpEx.Message}", 500);
            }
        }

        private string ExtractRealUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            int lastHttpIndex = url.LastIndexOf("http", StringComparison.OrdinalIgnoreCase);
            if (lastHttpIndex > 0)
            {
                return url.Substring(lastHttpIndex);
            }
            return url;
        }
    }
}