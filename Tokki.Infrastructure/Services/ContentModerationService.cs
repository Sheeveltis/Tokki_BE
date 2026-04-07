using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        private readonly ILogger<ContentModerationService> _logger;

        public ContentModerationService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options, ILogger<ContentModerationService> logger = null)
        {
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ModerationResult> CheckContentAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new ModerationResult { IsClean = true };

            var config = _options.Blog; // Dùng config riêng cho Blog moderation
            var apiKey = !string.IsNullOrEmpty(config.ApiKey) ? config.ApiKey : _options.ApiKey;
            var baseUrl = !string.IsNullOrEmpty(config.BaseUrl) ? config.BaseUrl : "https://generativelanguage.googleapis.com/v1beta";
            var model = !string.IsNullOrEmpty(config.Model) ? config.Model : "gemini-2.5-flash";

            if (string.IsNullOrEmpty(apiKey))
            {
                // Fallback nếu không cấu hình AI
                return new ModerationResult { IsClean = true };
            }

            string prompt = $@"
Bạn là một công cụ kiểm duyệt nội dung (Content Moderation AI) khắt khe của hệ thống giáo dục Tokki.
Nhiệm vụ của bạn là phân tích đoạn văn bản dưới đây và xác định xem nó có chứa nội dung vi phạm hay không.
Khung vi phạm:
1. Từ ngữ thô tục, chửi thề, lăng mạ (tiếng Việt, Anh, Hàn), đặc biệt là các từ/cụm ký tự viết tắt lóng mạng (ví dụ: vcl, đm, ㅅㅂ, ㅈㄴ).
2. Từ ngữ chính trị nhạy cảm, chống phá, xúc phạm danh nhân/thể chế.
3. Nội dung khiêu dâm, bạo lực.

Đoạn văn bản cần kiểm tra:
""{content}""

Chỉ trả về JSON hợp lệ duy nhất với cấu trúc sau:
{{
    ""isClean"": true, // hoặc false nếu có vi phạm
    ""badWordsFound"": [] // mảng string chứa các từ hoặc cụm từ vi phạm trích xuất được từ văn bản, rỗng nếu isClean là true
}}";

            var url = $"{baseUrl.TrimEnd('/')}/models/{model}:generateContent?key={apiKey}";
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { response_mime_type = "application/json" }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Gemini Moderation API FAILED: {StatusCode} - {Error}", response.StatusCode, errorDetails);
                    
                    return new ModerationResult 
                    { 
                        IsClean = false, 
                        IsError = true, 
                        ErrorMessage = $"Dịch vụ AI đang gặp sự cố ({response.StatusCode})" 
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return new ModerationResult { IsClean = true };

                var rawJson = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                rawJson = rawJson?.Replace("```json", "").Replace("```", "").Trim();

                var moderationData = JsonSerializer.Deserialize<JsonElement>(rawJson!);

                bool isClean = moderationData.GetProperty("isClean").GetBoolean();
                
                var badWordsFound = new List<string>();
                if (moderationData.TryGetProperty("badWordsFound", out var badWordsArray) && badWordsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var word in badWordsArray.EnumerateArray())
                    {
                        var wordStr = word.GetString();
                        if (!string.IsNullOrEmpty(wordStr))
                        {
                            badWordsFound.Add(wordStr);
                        }
                    }
                }

                return new ModerationResult 
                { 
                    IsClean = isClean, 
                    BadWordsFound = badWordsFound 
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Content Moderation Gemini Exception: {Message}", ex.Message);
                return new ModerationResult 
                { 
                    IsClean = false, 
                    IsError = true, 
                    ErrorMessage = "Không thể kết nối đến bộ lọc tự động." 
                };
            }
        }
    }
}
