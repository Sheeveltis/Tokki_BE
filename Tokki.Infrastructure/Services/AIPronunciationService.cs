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

        public async Task<(string Feedback, double FinalAccuracyScore)> GenerateFeedbackWithAudioAsync(
            PronunciationAssessmentDTO assessment,
            string targetText,
            string ruleContext,
            string base64Audio,
            string mimeType)
        {
            var config = _options.Pronunciation;
            if (string.IsNullOrEmpty(config.ApiKey)) return ("Lỗi cấu hình hệ thống.", assessment.AccuracyScore);

            var syllableDetails = assessment.Words
                .Select(w => $"- Từ '{w.Word}': Điểm chi tiết âm tiết [{w.Phonemes}]")
                .ToList();
            string detailedInfo = string.Join("\n", syllableDetails);

            string prompt = $@"
            Bạn là chuyên gia ngôn ngữ Hàn Quốc tích hợp trong hệ thống Tokki. Hãy phân tích file âm thanh và đối chiếu với:
            - Câu mẫu: '{targetText}'
            - Quy tắc trọng tâm: {ruleContext}
            - Điểm hệ thống nhận diện sơ bộ: 
            {detailedInfo}

            Nhiệm vụ:
            1. Nghe trực tiếp file audio để thẩm định xem người học thực tế có áp dụng đúng quy tắc '{ruleContext}' hay không.
            2. Đối chiếu: Nếu điểm âm tiết cao nhưng nghe audio thấy người học đọc ngắt quãng, không nối âm/biến âm theo đúng quy tắc, hãy thực hiện trừ từ 15-25 điểm (Penalty).
            3. Nếu người học làm tốt và đúng quy tắc, penalty sẽ là 0.
            4. Trả về kết quả theo định dạng JSON:
            {{
                ""penalty"": <số điểm trừ>,
                ""comment"": ""<nhận xét bằng tiếng Việt ngắn gọn, khách quan, chỉ ra lỗi cụ thể, xưng hô 'Tokki' và gọi người học là 'Bạn', tối đa 3 câu.>""
            }}";

            var url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.Model}:generateContent?key={config.ApiKey}";

            var payload = new
            {
                contents = new[] {
            new {
                parts = new object[] {
                    new { text = prompt },
                    new { inlineData = new { mimeType = mimeType, data = base64Audio } }
                }
            }
        },
                generationConfig = new { response_mime_type = "application/json" }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                {
                    // LỚP PHÒNG NGỰ 1: In ra lỗi nếu xịt để biết do mạng hay do API Key
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini Audio API FAILED: {response.StatusCode} - {errorDetails}");
                    return ("Tokki nhận thấy hệ thống AI đang quá tải hoặc hết lượt dùng, bạn vui lòng thử lại sau nhé.", assessment.AccuracyScore);
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return ("Phân tích bị từ chối hoặc không có kết quả. Bạn hãy thử thu âm rõ hơn xem sao.", assessment.AccuracyScore);
                }

                var rawJson = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                rawJson = rawJson?.Replace("```json", "").Replace("```", "").Trim();

                var feedbackData = JsonSerializer.Deserialize<JsonElement>(rawJson!);

                int penalty = 0;
                if (feedbackData.TryGetProperty("penalty", out var p)) penalty = p.GetInt32();

                string comment = "Tokki đánh giá bài làm của bạn rất tốt.";
                if (feedbackData.TryGetProperty("comment", out var c)) comment = c.GetString()!;

                double finalScore = Math.Max(0, assessment.AccuracyScore - penalty);

                return (comment, finalScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini Feedback Error: {ex.Message}");
                return ("Tokki đang phân tích lại bài làm của bạn, vui lòng thử lại sau giây lát.", assessment.AccuracyScore);
            }
        }
    }
}