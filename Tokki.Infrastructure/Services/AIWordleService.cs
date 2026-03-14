using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services
{
    public class AIWordleService : IAIWordleService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public AIWordleService(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<WordleAiFeedbackDto> EvaluateSentenceAsync(string sentence, string word, string definition)
        {
            var config = _options.Wordle;

            if (string.IsNullOrEmpty(config.BaseUrl) || string.IsNullOrEmpty(config.ApiKey))
            {
                return new WordleAiFeedbackDto { GeneralFeedback = "Lỗi cấu hình hệ thống (API Key/URL null)." };
            }
            var baseUrl = config.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/models/{config.Model}:generateContent?key={config.ApiKey}";

            var prompt = $@"
            Bạn là một giám khảo chấm thi TOPIK (Test of Proficiency in Korean) nổi tiếng khắt khe. 
            Nhiệm vụ: Chấm điểm câu đặt của sinh viên dựa trên từ khóa cho trước.

            THÔNG TIN:
            - Từ khóa mục tiêu: '{word}'
            - Nghĩa chuẩn: {definition}
            - Câu của sinh viên: '{sentence}'

            QUY TẮC CHẤM ĐIỂM (TỔNG 100 ĐIỂM):

            1. Meaning (40đ): 
               - 40đ: Sử dụng đúng nghĩa, ngữ cảnh tự nhiên.
               - 10-20đ: Dùng từ bị gượng ép hoặc sai sắc thái nghĩa.
               - 0đ: Sai nghĩa hoàn toàn hoặc không có từ khóa.

            2. Grammar & Complexity (30đ): 
               - [Sơ cấp 1]: (Chỉ dùng -이/가, -은/는, -에, -아요/어요): TỐI ĐA 10đ.
               - [Sơ cấp 2 - Trung cấp 1]: (Dùng -(으)니까, -아/어서, -지만, -고...): TỐI ĐA 20đ.
               - [Trung cấp 2 - Cao cấp]: (Dùng định ngữ phức tạp, cấu trúc -(으)ㄹ 뿐만 아니라, -기 nhờ..., kính ngữ -시...): TỐI ĐA 30đ.

            3. Naturalness & Depth (30đ): 
               - Câu dưới 8 từ: TỐI ĐA 10đ. 
               - Câu có trạng từ (매우, nhất là...), định ngữ, bối cảnh rõ ràng: 20-30đ.

            VÍ DỤ THAM CHIẾU:
            - Từ khóa '사과' (Táo). Câu '저는 사과를 먹어요' (Tôi ăn táo) -> Tổng: 45-50đ (Quá đơn giản).
            - Câu '시장에서 빨간 사과를 세 개 샀어요' (Tôi đã mua 3 quả táo đỏ ở chợ) -> Tổng: 65-70đ.
            - Câu '건강을 위해 매일 아침 사과를 한 알씩 챙겨 먹는 습관을 기르고 있어요' (Tôi đang tập thói quen mỗi sáng ăn một quả táo vì sức khỏe) -> Tổng: 90-95đ.

            LƯU Ý: Nếu câu vi phạm tiêu chí độ dài hoặc ngữ pháp sơ cấp, tuyệt đối không cho tổng điểm quá 60.

            YÊU CẦU ĐẦU RA (JSON THUẦN TÚY):
            {{
                ""ContainsTargetWord"": true/false,
                ""TotalScore"": <Tổng điểm thực tế>,
                ""Meaning"": {{ ""Score"": <số>, ""MaxScore"": 40, ""Feedback"": ""Nhận xét nghĩa"" }},
                ""Grammar"": {{ ""Score"": <số>, ""MaxScore"": 30, ""Feedback"": ""Nhận xét cấu trúc ngữ pháp"" }},
                ""Naturalness"": {{ ""Score"": <số>, ""MaxScore"": 30, ""Feedback"": ""Nhận xét độ dài và tính tự nhiên"" }},
                ""GeneralFeedback"": ""Giải thích vì sao câu này đạt mức Sơ cấp/Trung cấp/Cao cấp"",
                ""CorrectedSentence"": ""Viết lại 1 câu ở trình độ CAO CẤP (90+ điểm) để học sinh học hỏi""
            }}";

            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new WordleAiFeedbackDto { GeneralFeedback = "Lỗi kết nối AI." };
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return new WordleAiFeedbackDto { GeneralFeedback = "Câu của bạn bị hệ thống AI từ chối phân tích vì lý do an toàn." };
                }

                var rawText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                string cleanedJson = CleanJson(rawText!);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<WordleAiFeedbackDto>(cleanedJson, options) ?? new WordleAiFeedbackDto();
            }
            catch (Exception ex)
            {
                return new WordleAiFeedbackDto { GeneralFeedback = "Hệ thống đang bận, vui lòng thử lại." };
            }
        }

        private string CleanJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

            string cleaned = input.Trim();
            if (cleaned.StartsWith("```"))
            {
                int firstBrace = cleaned.IndexOf('{');
                int lastBrace = cleaned.LastIndexOf('}');
                if (firstBrace != -1 && lastBrace != -1)
                {
                    cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
            }
            return cleaned;
        }
    }
}