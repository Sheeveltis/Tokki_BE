using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Blogs.Commands.GenerateBlogCover
{
    public class GenerateBlogCoverCommandHandler : IRequestHandler<GenerateBlogCoverCommand, OperationResult<byte[]>>
    {
        private readonly IImageSearchService _imageSearchService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GenerateBlogCoverCommandHandler> _logger;
        private const string MascotUrl = "https://res.cloudinary.com/dxfii0v3c/image/upload/v1776304074/THTH1-1_oxix7f.png";

        // Cache mascot in memory to avoid repetitive downloads
        private static string? _cachedMascotBase64;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public GenerateBlogCoverCommandHandler(
            IImageSearchService imageSearchService,
            IHttpClientFactory httpClientFactory,
            ILogger<GenerateBlogCoverCommandHandler> logger)
        {
            _imageSearchService = imageSearchService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Warm up mascot cache in background
            _ = EnsureMascotCachedAsync();
        }

        private async Task EnsureMascotCachedAsync()
        {
            if (!string.IsNullOrEmpty(_cachedMascotBase64)) return;

            try
            {
                await _lock.WaitAsync();
                try
                {
                    if (string.IsNullOrEmpty(_cachedMascotBase64))
                    {
                        using var client = _httpClientFactory.CreateClient();
                        var mascotBytes = await client.GetByteArrayAsync(MascotUrl);
                        _cachedMascotBase64 = Convert.ToBase64String(mascotBytes);
                        _logger.LogInformation("Mascot cache warmed up successfully.");
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up mascot cache: {Message}", ex.Message);
            }
        }

        public async Task<OperationResult<byte[]>> Handle(GenerateBlogCoverCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return OperationResult<byte[]>.Failure("Tiêu đề không được để trống", 400);
            }

            _logger.LogInformation("Bắt đầu tạo ảnh bìa cho blog: {Title}", request.Title);

            try
            {
                // 1. Đảm bảo Mascot đã có trong Cache
                await EnsureMascotCachedAsync();

                if (string.IsNullOrEmpty(_cachedMascotBase64))
                {
                    return OperationResult<byte[]>.Failure("Không thể tải mascot để tạo ảnh", 500);
                }

                // 2. Dùng Gemini để tạo ảnh bìa
                _logger.LogInformation("Đang yêu cầu Gemini tạo ảnh bìa...");
                var coverBytes = await _imageSearchService.GenerateBlogCoverAsync(request.Title, _cachedMascotBase64);

                if (coverBytes == null || coverBytes.Length == 0)
                {
                    _logger.LogWarning("Gemini không trả về ảnh hoặc ảnh rỗng.");
                    return OperationResult<byte[]>.Failure("Không thể tạo ảnh bìa bằng AI", 500);
                }
                _logger.LogInformation("Gemini đã tạo ảnh thành công, dung lượng: {Size} bytes", coverBytes.Length);

                // 3. Trả về mảng bytes trực tiếp
                _logger.LogInformation("Tạo ảnh bìa hoàn tất (trả về Binary Stream)");
                return OperationResult<byte[]>.Success(coverBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi tạo ảnh bìa: {Message}", ex.Message);
                return OperationResult<byte[]>.Failure($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }
    }
}
