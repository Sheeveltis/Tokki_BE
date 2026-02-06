using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class AiRoadmapService : IAiRoadmapService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiRoadmapService> _logger;
        private readonly string _apiKey;

        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"; 
        public AiRoadmapService(HttpClient httpClient, ILogger<AiRoadmapService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AiSettings:ApiKey"];
        }

        public async Task<AiRoadmapResponse?> GenerateStudyPlanAsync(string target, int days, List<string> weaknesses)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("API Key chưa được cấu hình trong appsettings.json");
                return null;
            }

            var weaknessStr = (weaknesses != null && weaknesses.Count > 0) ? string.Join(", ", weaknesses) : "Không có";

            var promptText = $@"
                Đóng vai chuyên gia TOPIK. Tạo lộ trình {days} ngày. Mục tiêu: {target}. Điểm yếu: {weaknessStr}.
                Quan trọng: Chỉ trả về JSON thuần (no markdown), theo cấu trúc sau:
                {{
                  ""Assessment"": ""Nhận xét ngắn"",
                  ""Weeks"": [
                    {{
                      ""WeekIndex"": 1,
                      ""WeekGoal"": ""Mục tiêu tuần 1"",
                      ""Days"": [
                        {{
                          ""DayIndex"": 1,
                          ""Tasks"": [
                             {{ ""Title"": ""Tên bài"", ""TaskType"": ""LearnTheory"", ""Content"": ""Nội dung HTML"" }},
                             {{ ""Title"": ""Quiz"", ""TaskType"": ""VirtualQuiz"", ""Content"": ""JSON Quiz"" }}
                          ]
                        }}
                      ]
                    }}
                  ]
                }}
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = promptText } } } },
                generationConfig = new { responseMimeType = "application/json" } 
            };

            try
            {
                var url = $"{BASE_URL}?key={_apiKey}";
                _logger.LogInformation($"Đang gọi AI tới URL: {BASE_URL}");

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi gọi AI ({response.StatusCode}): {errorContent}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (jsonResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    text = text.Replace("```json", "").Replace("```", "").Trim();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<AiRoadmapResponse>(text, options);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi gọi AI Service");
                return null;
            }
        }
    }
}