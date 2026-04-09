using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.AutoFindImages
{
    public class AutoFindVocabularyImagesCommandHandler
        : IRequestHandler<AutoFindVocabularyImagesCommand, OperationResult<ExportFileDTO>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IImageSearchService _imageSearchService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IExcelService _excelService;
        private readonly ILogger<AutoFindVocabularyImagesCommandHandler> _logger;

        public AutoFindVocabularyImagesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IImageSearchService imageSearchService,
            ICloudinaryService cloudinaryService,
            IExcelService excelService,
            ILogger<AutoFindVocabularyImagesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _imageSearchService = imageSearchService;
            _cloudinaryService = cloudinaryService;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<OperationResult<ExportFileDTO>> Handle(
            AutoFindVocabularyImagesCommand request,
            CancellationToken cancellationToken)
        {
            if (request.VocabularyIds == null || !request.VocabularyIds.Any())
            {
                return OperationResult<ExportFileDTO>.Failure("Danh sách VocabularyId không được rỗng", 400);
            }

            // 1. Lấy danh sách vocabulary từ DB
            var vocabularies = await _vocabularyRepository.GetByIdsAsync(request.VocabularyIds);
            if (vocabularies == null || !vocabularies.Any())
            {
                return OperationResult<ExportFileDTO>.Failure("Không tìm thấy từ vựng nào với danh sách ID đã cho", 404);
            }

            var results = new List<VocabularyImageResultDto>();

            // 2. Xử lý từng vocabulary
            var vocabIndex = 0;
            foreach (var vocab in vocabularies)
            {
                var result = new VocabularyImageResultDto
                {
                    VocabularyId = vocab.VocabularyId,
                    Text = vocab.Text,
                    Definition = vocab.Definition,
                    OriginalImgURL = vocab.ImgURL
                };

                try
                {
                    // Bỏ qua nếu đã có ảnh và không cho phép ghi đè
                    if (!string.IsNullOrEmpty(vocab.ImgURL) && !request.OverwriteExisting)
                    {
                        result.Status = "Skipped";
                        result.NewImgURL = vocab.ImgURL;
                        result.ErrorMessage = "Đã có ảnh, bỏ qua (OverwriteExisting = false)";
                        results.Add(result);
                        continue;
                    }

                    // ===== CHIẾN LƯỢC: AI sinh ảnh (ưu tiên) + Pixabay (fallback) =====
                    string aiCloudUrl = null;
                    string pixabayCloudUrl = null;

                    // --- Cột 1: AI SINH ẢNH (Gemini Imagen) ---
                    vocabIndex++;
                    Console.WriteLine($"\n🎨 [{vocabIndex}/{vocabularies.Count()}] '{vocab.Text}' ({vocab.Definition})");
                    var aiImageBytes = await _imageSearchService.GenerateImageForVocabAsync(
                        vocab.Definition, vocab.Text);

                    if (aiImageBytes != null && aiImageBytes.Length > 0)
                    {
                        try
                        {
                            aiCloudUrl = await _cloudinaryService.UploadImageFromBytesAsync(
                                aiImageBytes, $"ai_{vocab.VocabularyId}", "tokki/vocab-ai");
                            Console.WriteLine($"  ☁️ AI → Cloudinary: {(aiCloudUrl != null ? "✅" : "❌")}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Upload AI ảnh thất bại: {Msg}", ex.Message);
                        }
                    }

                    // --- Cột 2: PIXABAY SEARCH (fallback nếu AI fail, hoặc để so sánh) ---
                    var pixabayUrls = await _imageSearchService.SearchImagesForVocabAsync(
                        vocab.Definition, vocab.Text, 3);
                    if (pixabayUrls.Any())
                    {
                        foreach (var url in pixabayUrls)
                        {
                            try
                            {
                                pixabayCloudUrl = await _cloudinaryService.UploadImageFromUrlAsync(
                                    url, "tokki/vocab-image");
                                if (!string.IsNullOrEmpty(pixabayCloudUrl)) break;
                            }
                            catch (Exception ex) { _logger.LogWarning("Upload Pixabay thất bại: {Msg}", ex.Message); }
                        }
                    }

                    Console.WriteLine($"  📊 AI: {(aiCloudUrl != null ? "✅" : "❌")} | Pixabay: {(pixabayCloudUrl != null ? "✅" : "❌")}");

                    result.ViImgURL = aiCloudUrl;        // Cột "Ảnh AI" trong Excel
                    result.KoImgURL = pixabayCloudUrl;   // Cột "Ảnh Pixabay" trong Excel

                    // Ưu tiên ảnh AI vì chính xác hơn, fallback Pixabay
                    var chosenUrl = aiCloudUrl ?? pixabayCloudUrl;

                    if (string.IsNullOrEmpty(chosenUrl))
                    {
                        result.Status = "Failed";
                        result.ErrorMessage = "Không tạo/tìm được ảnh nào.";
                        results.Add(result);
                        continue;
                    }

                    vocab.ImgURL = chosenUrl;
                    vocab.UpdateDate = DateTime.UtcNow;
                    await _vocabularyRepository.UpdateAsync(vocab);

                    result.Status = "Success";
                    result.NewImgURL = chosenUrl;
                    result.ErrorMessage = $"AI: {(aiCloudUrl != null ? "✅" : "❌")} | Pixabay: {(pixabayCloudUrl != null ? "✅" : "❌")}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xử lý vocabulary {VocabId}", vocab.VocabularyId);
                    result.Status = "Failed";
                    result.ErrorMessage = ex.Message;
                }

                results.Add(result);

                // Rate limiting: đợi 500ms giữa mỗi request tìm ảnh
                await Task.Delay(500, cancellationToken);
            }

            // 6. Lưu thay đổi vào DB
            await _vocabularyRepository.SaveChangesAsync(cancellationToken);

            // 7. Xuất kết quả ra Excel (delegate sang IExcelService)
            var excelBytes = await _excelService.ExportVocabularyImageResultsToExcelAsync(results);

            var exportFile = new ExportFileDTO
            {
                FileName = $"VocabularyImages_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileContent = excelBytes
            };

            return OperationResult<ExportFileDTO>.Success(exportFile,
                message: $"Hoàn tất: {results.Count(r => r.Status == "Success")} thành công, " +
                         $"{results.Count(r => r.Status == "Failed")} thất bại, " +
                         $"{results.Count(r => r.Status == "Skipped")} bỏ qua");
        }

        /// <summary>
        /// Thử tuần tự từng query trong danh sách, trả về kết quả ngay khi tìm thấy ảnh.
        /// </summary>
        private async Task<List<string>> SearchWithFallbackAsync(string[] queries, CancellationToken cancellationToken)
        {
            foreach (var query in queries)
            {
                if (string.IsNullOrWhiteSpace(query)) continue;
                var urls = await _imageSearchService.SearchImagesAsync(query, 3);
                if (urls != null && urls.Any())
                {
                    _logger.LogInformation("[Search] Tìm thấy {Count} ảnh bằng query: '{Query}'", urls.Count, query);
                    return urls;
                }
                await Task.Delay(200, cancellationToken);
            }
            return new List<string>();
        }
    }
}
