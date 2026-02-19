// Infrastructure/Services/Gemini/GeminiRestClient.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.Gemini
{
    public sealed class GeminiRestClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiConfig _config;
        private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        public GeminiRestClient(IHttpClientFactory httpClientFactory, GeminiConfig config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
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

            if (string.IsNullOrWhiteSpace(_config.ApiKey))
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
                var baseUrl = _config.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/models/{_config.Model}:generateContent?key={_config.ApiKey}";

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
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

            // Strategy 1: Try direct parse
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                return doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"⚠️ JSON parse failed: {ex.Message}");

                // Strategy 2: Fix line breaks in strings
                var fixed1 = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    @"""([^""\\]*(?:\\.[^""\\]*)*)""",
                    match =>
                    {
                        var value = match.Groups[1].Value;
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
                catch { }

                // Strategy 3: Extract JSON object
                var start = cleaned.IndexOf('{');
                var end = cleaned.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    var slice = cleaned[start..(end + 1)];

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

                    try
                    {
                        using var doc = JsonDocument.Parse(slice);
                        return doc.RootElement.Clone();
                    }
                    catch (JsonException ex4)
                    {
                        Console.WriteLine($"⚠️ Final strategy failed: {ex4.Message}");
                        throw;
                    }
                }

                throw new JsonException($"All parsing strategies failed. Original error: {ex.Message}");
            }
        }
    }
}