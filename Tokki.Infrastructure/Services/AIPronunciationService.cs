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
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services
{
    public class AIPronunciationService : IAIPronunciationService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public AIPronunciationService(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
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

            string prompt = $@"
            Bạn là chuyên gia ngôn ngữ Hàn Quốc tích hợp trong hệ thống Tokki. Hãy phân tích dữ liệu phát âm:
            - Câu mẫu: '{targetText}'
            - Quy tắc trọng tâm: {ruleContext}
            - Dữ liệu từ Azure:
            {detailedInfo}

            Nhiệm vụ:
            1. ĐÁNH GIÁ ĐỘ TÍN NHIỆM: Nếu điểm AccuracyScore của hầu hết các từ đều rất thấp (< 40), hãy nhận định rằng người học phát âm chưa rõ chữ hoặc đọc sai kịch bản. Trong trường hợp này, phần 'generalFeedback' chỉ cần khuyên người học đọc lại chậm rãi, KHÔNG CẦN hướng dẫn sửa lỗi từng từ (để rỗng mảng wordFeedbacks).
            2. Đưa ra nhận xét tổng thể (generalFeedback) về cả câu (2-3 câu).
            3. Với mỗi từ có AccuracyScore từ 40 đến 79, hãy đưa ra hướng dẫn sửa lỗi (repairGuide) ngắn gọn.
            4. Nếu vi phạm quy tắc '{ruleContext}', hãy trừ từ 10-20 điểm (penalty).
            5. Trả về JSON:
            {{
                ""penalty"": <số điểm trừ>,
                ""generalFeedback"": ""<nhận xét tổng thể, xưng Tokki gọi Bạn>"",
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