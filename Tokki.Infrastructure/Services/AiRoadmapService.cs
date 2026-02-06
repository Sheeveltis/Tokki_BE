using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    public class AiRoadmapService : IAiRoadmapService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiRoadmapService> _logger;

        // Bạn nên chuyển 2 key này vào appsettings.json sau này
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
        private const string API_KEY = "YOUR_OPENAI_API_KEY"; // Thay key thật của bạn vào đây

        public AiRoadmapService(HttpClient httpClient, ILogger<AiRoadmapService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AiRoadmapResponse?> GenerateStudyPlanAsync(string target, int days, List<string> weaknesses)
        {
            var weaknessStr = weaknesses != null && weaknesses.Count > 0
                ? string.Join(", ", weaknesses)
                : "None";

            var systemPrompt = @"You are a TOPIK exam expert. Create a study roadmap.
Output strictly in JSON format matching this structure:
{
  ""Assessment"": ""Review based on weaknesses"",
  ""Weeks"": [
    {
      ""WeekIndex"": 1,
      ""WeekGoal"": ""Goal of week"",
      ""Days"": [
        {
          ""DayIndex"": 1,
          ""Tasks"": [
            { ""Title"": ""Task name"", ""TaskType"": ""LearnTheory"", ""Content"": ""HTML content"" },
            { ""Title"": ""Practice"", ""TaskType"": ""VirtualQuiz"", ""Content"": ""JSON quiz data"" }
          ]
        },
        ... (Day 7 MUST be TaskType: 'WeeklyExam')
      ]
    }
  ]
}";

            var userPrompt = $"Create a {days}-day roadmap for target '{target}'. Weaknesses: {weaknessStr}.";

            var requestBody = new
            {
                model = "gpt-3.5-turbo", 
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                var contentString = jsonResponse.GetProperty("choices")[0]
                                                .GetProperty("message")
                                                .GetProperty("content")
                                                .GetString();

                if (string.IsNullOrEmpty(contentString)) return null;

                contentString = contentString.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roadmapData = JsonSerializer.Deserialize<AiRoadmapResponse>(contentString, options);

                return roadmapData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi AI Service để tạo lộ trình.");
                return null;
            }
        }
    }
}