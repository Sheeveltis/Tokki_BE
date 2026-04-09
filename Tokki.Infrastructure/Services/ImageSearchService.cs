using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    /// <summary>
    /// Luồng xử lý ảnh từ vựng:
    /// 1. AI Gen (Gemini Imagen) — sinh ảnh từ prompt VI + KO → chính xác nhất
    /// 2. Pixabay (fallback)     — Gemini dịch VI+KO → EN keyword → search Pixabay
    /// </summary>
    public class ImageSearchService : IImageSearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageSearchService> _logger;
        private readonly string _pixabayApiKey;
        private readonly string _geminiApiKey;
        private readonly string _geminiBaseUrl;

        public ImageSearchService(
            IHttpClientFactory httpClientFactory,
            ILogger<ImageSearchService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _pixabayApiKey = configuration["PixabaySettings:ApiKey"]
                ?? throw new InvalidOperationException("PixabaySettings:ApiKey chưa được cấu hình.");
            _geminiApiKey = configuration["Gemini:Writing:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:Writing:ApiKey chưa được cấu hình.");
            _geminiBaseUrl = configuration["Gemini:Writing:BaseUrl"]
                ?? "https://generativelanguage.googleapis.com/v1beta/";
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  1. AI SINH ẢNH (Gemini Imagen) — Ưu tiên số 1                ║
        // ╚══════════════════════════════════════════════════════════════════╝
        public async Task<byte[]?> GenerateImageForVocabAsync(string viDefinition, string koText)
        {
            Console.WriteLine($"\n🎨 [AI Gen] VI='{viDefinition}' | KO='{koText}'");

            var models = new[]
            {
                "gemini-2.5-flash-image",
                "gemini-3.1-flash-image-preview",
                "gemini-3-pro-image-preview",
            };

            foreach (var model in models)
            {
                Console.WriteLine($"  [AI Gen] Thử model: {model}");
                var result = await TryGenerateWithModelAsync(model, viDefinition, koText);
                if (result != null) return result;
            }

            Console.WriteLine("  [AI Gen] ❌ Tất cả model đều thất bại.");
            return null;
        }

        private async Task<byte[]?> TryGenerateWithModelAsync(string model, string viDef, string koText)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_geminiBaseUrl}models/{model}:generateContent?key={_geminiApiKey}";

                // Prompt chi tiết: gửi cả tiếng Việt + tiếng Hàn + yêu cầu rõ ràng
                var prompt =
                    "You are an expert at creating vocabulary flashcard illustrations.\n\n" +
                    "VOCABULARY INFORMATION:\n" +
                    $"• Korean (한국어): {koText}\n" +
                    $"• Vietnamese meaning (Tiếng Việt): {viDef}\n\n" +
                    "YOUR TASK:\n" +
                    "Create ONE illustration that clearly represents the meaning of this vocabulary word.\n" +
                    "A student looking at this image should immediately understand what the word means.\n\n" +
                    "STYLE REQUIREMENTS:\n" +
                    "• Simple, clean, flat illustration style (like Duolingo or language learning apps)\n" +
                    "• White or very light solid background\n" +
                    "• NO text, NO letters, NO words, NO numbers in the image\n" +
                    "• Colorful and visually appealing\n" +
                    "• Show the CONCEPT clearly — e.g., for '어디' (where/ở đâu), show a person with a question mark looking around; " +
                    "for '쇼핑하다' (shopping/mua sắm), show a person holding shopping bags\n" +
                    "• Square aspect ratio (1:1)\n\n" +
                    "Generate the illustration now.";

                var requestBody = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        responseModalities = new[] { "IMAGE", "TEXT" },
                        maxOutputTokens = 1024
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                Console.WriteLine($"  POST: {url.Replace(_geminiApiKey, "***")}");

                var resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"  Status: {resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    // In lỗi ngắn gọn
                    Console.WriteLine($"  ❌ Error: {body.Substring(0, Math.Min(300, body.Length))}");
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("candidates", out var cands) || cands.GetArrayLength() == 0)
                {
                    Console.WriteLine("  ❌ Không có candidates");
                    return null;
                }

                var parts = cands[0].GetProperty("content").GetProperty("parts");

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("inlineData", out var inline))
                    {
                        var mime = inline.GetProperty("mimeType").GetString();
                        var b64 = inline.GetProperty("data").GetString();
                        Console.WriteLine($"  ✅ Ảnh AI: mime={mime}, size={b64?.Length} chars");
                        if (!string.IsNullOrEmpty(b64))
                            return Convert.FromBase64String(b64);
                    }
                    else if (part.TryGetProperty("text", out var txt))
                    {
                        Console.WriteLine($"  📝 Text: {(txt.GetString() ?? "").Substring(0, Math.Min(150, (txt.GetString() ?? "").Length))}");
                    }
                }

                Console.WriteLine("  ❌ Không có inlineData");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Exception: {ex.Message}");
                return null;
            }
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  2. PIXABAY SEARCH (Fallback) — Gemini dịch VI+KO → EN        ║
        // ║     Khi AI Gen thất bại, dùng Gemini để tìm từ tiếng Anh      ║
        // ║     phù hợp nhất, rồi search Pixabay bằng từ đó               ║
        // ╚══════════════════════════════════════════════════════════════════╝
        public async Task<List<string>> SearchImagesForVocabAsync(
            string viDefinition, string koText, int count = 1)
        {
            Console.WriteLine($"\n🔍 [Pixabay Fallback] VI='{viDefinition}' | KO='{koText}'");

            // Bước 1: Dùng AI dịch VI+KO → từ tiếng Anh phù hợp nhất
            var englishKeyword = await GetBestEnglishKeywordAsync(viDefinition, koText);
            Console.WriteLine($"  🌐 AI dịch → EN: '{englishKeyword}'");

            // Bước 2: Search Pixabay bằng từ tiếng Anh
            foreach (var imageType in new[] { "illustration", "photo" })
            {
                var results = await SearchPixabayAsync(englishKeyword, count, imageType);
                if (results.Any())
                {
                    Console.WriteLine($"  ✅ [{imageType}] Tìm thấy {results.Count} ảnh cho EN='{englishKeyword}'");
                    return results;
                }
            }

            // Bước 3: Fallback cuối — thử nghĩa tiếng Việt trực tiếp
            Console.WriteLine($"  🔄 Thử lại với VI: '{viDefinition}'");
            foreach (var imageType in new[] { "illustration", "photo" })
            {
                var results = await SearchPixabayAsync(viDefinition, count, imageType);
                if (results.Any()) return results;
            }

            Console.WriteLine("  ❌ Pixabay: Không tìm thấy ảnh nào.");
            return new List<string>();
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  3. TÌM ẢNH THÔ (Pixabay trực tiếp, giữ tương thích)         ║
        // ╚══════════════════════════════════════════════════════════════════╝
        public async Task<List<string>> SearchImagesAsync(string query, int count = 1)
        {
            foreach (var imageType in new[] { "illustration", "photo" })
            {
                var results = await SearchPixabayAsync(query, count, imageType);
                if (results.Any()) return results;
            }
            return new List<string>();
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  PRIVATE: Gemini dịch VI + KO → EN keyword tốt nhất           ║
        // ╚══════════════════════════════════════════════════════════════════╝
        private async Task<string> GetBestEnglishKeywordAsync(string viDefinition, string koText)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_geminiBaseUrl}models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";

                // Prompt rõ ràng: gửi cả 2 ngôn ngữ để AI hiểu đúng ngữ cảnh
                var prompt =
                    "You are a multilingual vocabulary expert and image search specialist.\n\n" +
                    "I have a Korean vocabulary word with its Vietnamese translation.\n" +
                    "I need to search for a PICTURE of this word on Pixabay (English image library).\n\n" +
                    $"Korean (한국어): {koText}\n" +
                    $"Vietnamese (Tiếng Việt): {viDefinition}\n\n" +
                    "Return ONLY 1-3 English keywords that would find the BEST matching image.\n" +
                    "Rules:\n" +
                    "- Use concrete, visual nouns when possible (e.g., 'lightning bolt' not 'electricity')\n" +
                    "- For verbs, describe the action visually (e.g., 'person shopping bags' for 'mua sắm/쇼핑하다')\n" +
                    "- For abstract words, find the most visual representation\n" +
                    "- Return ONLY the keywords, no explanation, no punctuation\n\n" +
                    "English keywords:";

                var requestBody = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                    generationConfig = new { maxOutputTokens = 20, temperature = 0.1 }
                };

                Console.WriteLine($"  [Gemini Translate] POST ...");
                var response = await client.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  [Gemini Translate] ❌ {response.StatusCode}");
                    return viDefinition;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var keyword = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim()?.Trim('"');

                Console.WriteLine($"  [Gemini Translate] ✅ '{viDefinition}' + '{koText}' → '{keyword}'");
                return string.IsNullOrWhiteSpace(keyword) ? viDefinition : keyword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Gemini Translate] ❌ Exception: {ex.Message}");
                return viDefinition;
            }
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  PRIVATE: Pixabay API                                          ║
        // ╚══════════════════════════════════════════════════════════════════╝
        private async Task<List<string>> SearchPixabayAsync(string query, int count, string imageType)
        {
            var results = new List<string>();
            var client = _httpClientFactory.CreateClient();
            var url = $"https://pixabay.com/api/?key={_pixabayApiKey}" +
                      $"&q={HttpUtility.UrlEncode(query)}&image_type={imageType}" +
                      $"&safesearch=true&order=popular&per_page={Math.Max(count, 3)}";

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return results;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("hits", out var hits)) return results;

                foreach (var item in hits.EnumerateArray())
                {
                    if (results.Count >= count) break;
                    var imgUrl = item.TryGetProperty("largeImageURL", out var l) ? l.GetString() : null;
                    imgUrl ??= item.TryGetProperty("webformatURL", out var w) ? w.GetString() : null;
                    if (!string.IsNullOrEmpty(imgUrl)) results.Add(imgUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Pixabay] Error searching '{Query}'", query);
            }
            return results;
        }
    }
}
