using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.DTOs;
using Tokki.Application.IRepositories;
using Tokki.Infrastructure.Configurations;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class AIPronunciationService : IAIPronunciationService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<AIPronunciationService> _logger;

        public AIPronunciationService(
            HttpClient httpClient, 
            IOptions<GeminiOptions> options, 
            ISystemConfigRepository systemConfigRepository,
            ILogger<AIPronunciationService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<(string GeneralFeedback, double FinalAccuracyScore)> GenerateFeedbackAsync(
     PronunciationAssessmentDTO assessment,
     string targetText,
     string ruleContext)
        {
            var config = _options.Pronunciation;
            if (string.IsNullOrEmpty(config.ApiKey)) return ("Lỗi cấu hình hệ thống.", assessment.AccuracyScore);

            var wordDetails = assessment.Words
                .Select(w => {
                    string syllables = w.Syllables != null && w.Syllables.Any()
                        ? string.Join(", ", w.Syllables.Select(s => $"{s.Syllable}: {s.AccuracyScore}đ"))
                        : "N/A";
                    return $"- Từ '{w.Word}': {w.AccuracyScore}đ. Chi tiết: [{syllables}]";
                })
                .ToList();

            string detailedInfo = string.Join("\n", wordDetails);

            // Lấy cấu hình chi tiết từ SystemConfig (dưới dạng JSON)
            string? dbConfigJson = await _systemConfigRepository.GetValueByKeyAsync("AI_PRONUNCIATION_PROMPT");
            var promptConfig = new PronunciationAiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    // Thử parse xem có phải JSON không, nếu không phải (là prompt cũ) thì sẽ dùng default
                    var parsedConfig = JsonSerializer.Deserialize<PronunciationAiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[AI Pronunciation] Key AI_PRONUNCIATION_PROMPT is not a valid JSON. Using defaults.");
                }
            }

            // Template cố định, chỉ chèn các phần cấu hình vào
            string prompt = $@"
Bạn là {promptConfig.Persona}. Hãy phân tích dữ liệu phát âm:
- Câu mẫu: '{targetText}'
- Quy tắc trọng tâm: {ruleContext}
- Dữ liệu từ Azure: {detailedInfo}

Nhiệm vụ:
1. ĐÁNH GIÁ ĐỘ TÍN NHIỆM: {promptConfig.ReliabilityCheck}
2. {promptConfig.GeneralFeedbackRules}
3. {promptConfig.RepairGuideRules}
4. {promptConfig.PenaltyRules.Replace("{ruleContext}", ruleContext)}

YÊU CẦU ĐẦU RA (JSON THUẦN TÚY):
{{
    ""penalty"": <số điểm trừ>,
    ""generalFeedback"": ""<nhận xét tổng thể>"",
    ""wordFeedbacks"": [
        {{ ""word"": ""<từ bị lỗi>"", ""repairGuide"": ""<cách sửa khẩu hình>"" }}
    ]
}}";

            var url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.Model}:generateContent?key={config.ApiKey}";
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
                    Console.WriteLine($"Gemini API FAILED: {response.StatusCode} - {errorDetails}");
                    return ("Hệ thống AI hiện đang quá tải hoặc hết lượt dùng. Bạn vui lòng thử lại sau.", assessment.AccuracyScore);
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return ("Hệ thống bận.", assessment.AccuracyScore);

                var rawJson = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                rawJson = rawJson?.Replace("```json", "").Replace("```", "").Trim();

                var feedbackData = JsonSerializer.Deserialize<JsonElement>(rawJson!);

                int penalty = feedbackData.GetProperty("penalty").GetInt32();
                string generalFeedback = feedbackData.GetProperty("generalFeedback").GetString()!;
                var wordFeedbacks = feedbackData.GetProperty("wordFeedbacks").EnumerateArray().ToList();

                foreach (var word in assessment.Words)
                {
                    var guide = wordFeedbacks.FirstOrDefault(f => f.GetProperty("word").GetString() == word.Word);
                    if (guide.ValueKind != JsonValueKind.Undefined)
                    {
                        word.IsFeedback = true;
                        word.RepairGuide = guide.GetProperty("repairGuide").GetString();
                    }
                }

                double finalScore = Math.Max(0, assessment.AccuracyScore - penalty);
                return (generalFeedback, finalScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini Error: {ex.Message}");
                return ("Tokki đang xử lý dữ liệu. Vui lòng thử lại.", assessment.AccuracyScore);
            }
        }
    }
}