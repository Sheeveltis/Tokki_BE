using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace Tokki.Infrastructure.Services.Gemini
{
    public sealed class GeminiRestClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiOptions _opt;
        private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        public GeminiRestClient(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> opt)
        {
            _httpClientFactory = httpClientFactory;
            _opt = opt.Value;
        }

        public async Task<(string base64, string mimeType)> DownloadImageWithMimeAsync(
      string imageUrl,
      CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL không được rỗng.", nameof(imageUrl));

            var http = _httpClientFactory.CreateClient("ImageDownload");

            try
            {
                using var response = await http.GetAsync(imageUrl, ct);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
                var base64 = Convert.ToBase64String(imageBytes);

                // ✅ Lấy MIME từ HTTP header
                var mimeType = response.Content.Headers.ContentType?.MediaType
                               ?? DetectMimeFromBytes(imageBytes)
                               ?? "image/jpeg";

                return (base64, mimeType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể tải ảnh từ URL: {imageUrl}", ex);
            }
        }

        private static string? DetectMimeFromBytes(byte[] bytes)
        {
            if (bytes.Length < 4) return null;

            // PNG
            if (bytes[0] == 0x89 && bytes[1] == 0x50 &&
                bytes[2] == 0x4E && bytes[3] == 0x47)
                return "image/png";

            // JPEG
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";

            // WebP
            if (bytes.Length >= 12 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 &&
                bytes[10] == 0x42 && bytes[11] == 0x50)
                return "image/webp";

            return null;
        }
        public async Task<string> GenerateContentAsync(
            IEnumerable<object> parts,
            string systemInstruction,
            int maxOutputTokens,
            double temperature,
            CancellationToken ct)
        {
            await _rateLimiter.WaitAsync(ct);
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < TimeSpan.FromSeconds(2))
                {
                    await Task.Delay(TimeSpan.FromSeconds(2) - timeSinceLastRequest, ct);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimiter.Release();
            }

            var apiKey = !string.IsNullOrWhiteSpace(_opt.ApiKey)
                ? _opt.ApiKey
                : Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Thiếu Gemini API key.");

            var body = new
            {
                system_instruction = new { parts = new object[] { new { text = systemInstruction } } },
                contents = new object[]
                {
                    new { role = "user", parts = parts.ToArray() }
                },
                generationConfig = new
                {
                    temperature,
                    maxOutputTokens,
                    responseMimeType = "application/json"
                }
            };

            var http = _httpClientFactory.CreateClient("Gemini");

            int maxRetries = 3;
            int retryDelayMs = 2000;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"models/{_opt.Model}:generateContent");
                req.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var resp = await http.SendAsync(req, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(raw);

                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (string.IsNullOrWhiteSpace(text))
                        throw new InvalidOperationException("Gemini trả về rỗng.");

                    return text!;
                }

                if ((int)resp.StatusCode == 429 && attempt < maxRetries)
                {
                    Console.WriteLine($"⚠️ Rate limit (429). Retry {attempt + 1}/{maxRetries} sau {retryDelayMs}ms...");
                    await Task.Delay(retryDelayMs, ct);
                    retryDelayMs *= 2;
                    continue;
                }

                throw new InvalidOperationException($"Gemini {(int)resp.StatusCode}: {raw}");
            }

            throw new InvalidOperationException("Gemini retry exhausted.");
        }

        public static JsonElement ParseJsonRobust(string maybeJson)
        {
            var cleaned = maybeJson.Trim();

            // Remove markdown fences
            if (cleaned.StartsWith("```json")) cleaned = cleaned[7..];
            if (cleaned.StartsWith("```")) cleaned = cleaned[3..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();

            // ✅ Strategy 1: Try direct parse
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                return doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"⚠️ JSON parse failed: {ex.Message}");
                Console.WriteLine($"⚠️ Attempting to fix line breaks in strings...");

                // ✅ Strategy 2: Fix line breaks inside string values
                // This regex finds string values and replaces real newlines with \n
                var fixed1 = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    @"""([^""\\]*(?:\\.[^""\\]*)*)""",
                    match =>
                    {
                        var value = match.Groups[1].Value;
                        // Replace real newlines with escaped newlines
                        value = value.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                        return $"\"{value}\"";
                    },
                    System.Text.RegularExpressions.RegexOptions.Singleline
                );

                try
                {
                    using var doc = JsonDocument.Parse(fixed1);
                    return doc.RootElement.Clone();
                }
                catch (JsonException ex2)
                {
                    Console.WriteLine($"⚠️ Still failed after line break fix: {ex2.Message}");
                }

                // ✅ Strategy 3: Fix Unicode escapes
                var fixed2 = System.Text.RegularExpressions.Regex.Replace(
                    fixed1,
                    @"\\u(?![0-9A-Fa-f]{4})",
                    ""
                );

                try
                {
                    using var doc = JsonDocument.Parse(fixed2);
                    return doc.RootElement.Clone();
                }
                catch (JsonException ex3)
                {
                    Console.WriteLine($"⚠️ Still failed after unicode fix: {ex3.Message}");
                }

                // ✅ Strategy 4: Extract JSON object and apply all fixes
                var start = cleaned.IndexOf('{');
                var end = cleaned.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    var slice = cleaned[start..(end + 1)];

                    // Fix line breaks in strings
                    slice = System.Text.RegularExpressions.Regex.Replace(
                        slice,
                        @"""([^""\\]*(?:\\.[^""\\]*)*)""",
                        match =>
                        {
                            var value = match.Groups[1].Value;
                            value = value.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
                            return $"\"{value}\"";
                        },
                        System.Text.RegularExpressions.RegexOptions.Singleline
                    );

                    // Fix unicode escapes
                    slice = System.Text.RegularExpressions.Regex.Replace(
                        slice,
                        @"\\u(?![0-9A-Fa-f]{4})",
                        ""
                    );

                    try
                    {
                        using var doc = JsonDocument.Parse(slice);
                        return doc.RootElement.Clone();
                    }
                    catch (JsonException ex4)
                    {
                        Console.WriteLine($"⚠️ Final strategy failed: {ex4.Message}");
                        Console.WriteLine($"⚠️ Problematic JSON snippet (first 500 chars):");
                        Console.WriteLine(slice.Substring(0, Math.Min(500, slice.Length)));
                        throw;
                    }
                }

                throw new JsonException($"All parsing strategies failed. Original error: {ex.Message}");
            }
        }

        private static string FixIncompleteJson(string json)
        {
            int openBraces = json.Count(c => c == '{');
            int closeBraces = json.Count(c => c == '}');
            int openBrackets = json.Count(c => c == '[');
            int closeBrackets = json.Count(c => c == ']');

            var result = json;

            for (int i = 0; i < openBrackets - closeBrackets; i++)
                result += "]";

            for (int i = 0; i < openBraces - closeBraces; i++)
                result += "}";

            return result;
        }
    }
}