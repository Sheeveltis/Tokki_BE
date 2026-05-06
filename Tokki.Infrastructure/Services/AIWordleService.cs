using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services
{
    public class AIWordleService : IAIWordleService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<AIWordleService> _logger;

        public AIWordleService(
            HttpClient httpClient, 
            IOptions<GeminiOptions> options, 
            ISystemConfigRepository systemConfigRepository,
            ILogger<AIWordleService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
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

            // Lấy cấu hình chi tiết từ SystemConfig (dưới dạng JSON)
            string? dbConfigJson = await _systemConfigRepository.GetValueByKeyAsync("AI_WORDLE_PROMPT");
            var promptConfig = new WordleAiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    var parsedConfig = JsonSerializer.Deserialize<WordleAiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AI Wordle] Failed to parse AI_WORDLE_PROMPT_CONFIG from DB. Using defaults.");
                }
            }

            // Xây dựng danh sách ví dụ
            string examplesPart = "";
            if (promptConfig.Examples != null && promptConfig.Examples.Count > 0)
            {
                foreach (var ex in promptConfig.Examples)
                {
                    examplesPart += $"- Từ khóa '{ex.Word}' ({ex.Definition}). Câu '{ex.Sentence}' -> Tổng: {ex.ScoreRange} ({ex.Feedback})\n";
                }
            }
            else
            {
                // Mặc định nếu không có ví dụ trong DB
                examplesPart = @"
- Từ khóa '사과' (Táo). Câu '저는 사과를 먹어요' (Tôi ăn táo) -> Tổng: 45-50đ (Quá đơn giản).
- Câu '시장에서 빨간 사과를 세 개 샀어요' (Tôi đã mua 3 quả táo đỏ ở chợ) -> Tổng: 65-70đ.
- Câu '건강을 위해 매일 아침 사과를 한 알씩 챙겨 먹는 습관을 기르고 있어요' (Tôi đang tập thói quen mỗi sáng ăn một quả táo vì sức khỏe) -> Tổng: 90-95đ.";
            }

            // Template cố định, chỉ chèn các phần cấu hình vào
            string prompt = $@"
Bạn là {promptConfig.Persona}. 

QUY TẮC QUAN TRỌNG:
{promptConfig.LanguageRules}

Nhiệm vụ: Chấm điểm câu đặt của sinh viên dựa trên từ khóa cho trước.

THÔNG TIN:
- Từ khóa mục tiêu: '{word}'
- Nghĩa chuẩn: {definition}
- Câu của sinh viên: '{sentence}'

QUY TẮC CHẤM ĐIỂM (TỔNG 100 ĐIỂM):

1. Meaning ({promptConfig.Meaning.MaxScore}đ): 
{promptConfig.Meaning.Description}

2. Grammar & Complexity ({promptConfig.Grammar.MaxScore}đ): 
{promptConfig.Grammar.Description}

3. Naturalness & Depth ({promptConfig.Naturalness.MaxScore}đ): 
{promptConfig.Naturalness.Description}

VÍ DỤ THAM CHIẾU:
{examplesPart}

LƯU Ý: 
- Nếu câu vi phạm tiêu chí độ dài hoặc ngữ pháp sơ cấp, tuyệt đối không cho tổng điểm quá {promptConfig.MaxScoreForSimpleSentence}.
- Tất cả các phần nhận xét (Feedback) và Giải thích (GeneralFeedback) BẮT BUỘC phải viết bằng TIẾNG VIỆT.

YÊU CẦU ĐẦU RA (JSON THUẦN TÚY):
{{
    ""ContainsTargetWord"": true/false,
    ""TotalScore"": <Tổng điểm thực tế, nếu không phải tiếng Hàn thì để 0>,
    ""Meaning"": {{ ""Score"": <số>, ""MaxScore"": {promptConfig.Meaning.MaxScore}, ""Feedback"": ""Nhận xét nghĩa bằng tiếng Việt"" }},
    ""Grammar"": {{ ""Score"": <số>, ""MaxScore"": {promptConfig.Grammar.MaxScore}, ""Feedback"": ""Nhận xét cấu trúc ngữ pháp bằng tiếng Việt"" }},
    ""Naturalness"": {{ ""Score"": <số>, ""MaxScore"": {promptConfig.Naturalness.MaxScore}, ""Feedback"": ""Nhận xét độ dài và tính tự nhiên bằng tiếng Việt"" }},
    ""GeneralFeedback"": ""Giải thích bằng tiếng Việt"",
    ""CorrectedSentence"": ""Viết lại 1 câu ở trình độ CAO CẤP (90+ điểm) bằng tiếng Hàn để học sinh học hỏi""
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