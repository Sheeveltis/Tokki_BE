using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs; 
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class AiRoadmapService : IAiRoadmapService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiRoadmapService> _logger;
        private readonly string _apiKey;
        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public AiRoadmapService(
            HttpClient httpClient,
            ILogger<AiRoadmapService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AiSettings:ApiKey"];
        }

        public async Task<AiRoadmapResponse?> GenerateStudyPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int durationDays,
            List<string> weaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,
            List<GrammarMenuItem> grammarMenu,
            List<QuestionTypeMenuItem> questionTypeMenu)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            var weaknessSection = weakTypeInfos.Any()
                ? string.Join("\n", weakTypeInfos.Select(w =>
                    $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})" +
                    (string.IsNullOrEmpty(w.Description) ? "" : $" — {w.Description}")))
                : "  - Không có điểm yếu cụ thể, học theo lộ trình chuẩn.";

            var grammarMenuJson = JsonSerializer.Serialize(
                grammarMenu.Select(g => new
                {
                    GrammarId = g.GrammarId,
                    Title = g.Title,
                    Syntaxes = g.Syntaxes,
                    Description = g.Description ?? "",
                    RelatedQuestionTypeId = g.RelatedQuestionTypeId ?? ""
                })
            );

            var quizMenuJson = JsonSerializer.Serialize(
                questionTypeMenu.Select(q => new
                {
                    QuestionTypeId = q.QuestionTypeId,
                    Code = q.Code,
                    Name = q.Name,
                    Skill = q.Skill,
                    Description = q.Description ?? ""
                })
            );

            var promptText = $@"
                Bạn là chuyên gia lập lộ trình TOPIK. Hãy tạo lộ trình {durationDays} ngày.
                - Mục tiêu: {target}
                - Trình độ hiện tại: {currentLevel}

            *** ĐIỂM YẾU CỦA HỌC VIÊN (BẮT BUỘC ƯU TIÊN) ***
                {weaknessSection}

            *** QUY TẮC BẮT BUỘC ***
                1. KHÔNG tự bịa bài học. CHỈ CHỌN từ MENU bên dưới.
                2. Task 'LearnTheory' → điền GrammarId từ GRAMMAR MENU.
                    Ưu tiên grammar có RelatedQuestionTypeId khớp với điểm yếu.
                3. Task 'VirtualQuiz' → điền QuestionTypeId từ QUIZ MENU.
                    Ưu tiên QuestionTypeId của các điểm yếu ở trên.
                4. Ngày thứ 7 phải có task 'WeeklyExam'.
                5. Mỗi ngày nên có cả LearnTheory lẫn VirtualQuiz để học xong luyện ngay.

            *** GRAMMAR MENU (dùng cho LearnTheory) ***
                {grammarMenuJson}

            *** QUIZ MENU (dùng cho VirtualQuiz) ***
                {quizMenuJson}

            *** OUTPUT JSON (chỉ JSON thuần, không markdown) ***
                {{
                    ""Assessment"": ""Nhận xét về trình độ và điểm yếu của học viên"",
                    ""Weeks"": [
                {{
                    ""WeekIndex"": 1,
                    ""WeekGoal"": ""Mục tiêu tuần 1"",
                    ""Days"": [
                {{
                    ""DayIndex"": 1,
                    ""Tasks"": [
                        {{
                            ""Title"": ""Tên bài"",
                            ""TaskType"": ""LearnTheory"",
                            ""Content"": ""Lời khuyên ngắn"",
                            ""GrammarId"": ""ID_TU_GRAMMAR_MENU""
                        }},
                    {{
                      ""Title"": ""Luyện tập"",
                      ""TaskType"": ""VirtualQuiz"",
                      ""Content"": ""Mô tả bài tập"",
                      ""QuestionTypeId"": ""ID_TU_QUIZ_MENU""
                    }}
                  ]
                }}
              ]
            }}
          ]
        }}
    ";
            var requestBody = new
            {
                contents = new[] {
            new { parts = new[] { new { text = promptText } } }
        }
            };
            try
            {
                var url = $"{BASE_URL}?key={_apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi gọi AI ({response.StatusCode}): {errorContent}");
                    return null;
                }
                using var jsonDoc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync());
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates)
                    && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Replace("```json", "").Replace("```", "").Trim();
                        int startIndex = text.IndexOf("{");
                        int endIndex = text.LastIndexOf("}");
                        if (startIndex >= 0 && endIndex > startIndex)
                            text = text.Substring(startIndex, endIndex - startIndex + 1);

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        return JsonSerializer.Deserialize<AiRoadmapResponse>(text, options);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception khi gọi AI Service");
                return null;
            }
        }
        public async Task<AiRoadmapResponse?> GenerateNextWeekPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int nextWeekIndex,
            int examScorePercent,
            List<string> reviewTypes,
            List<string> persistentFailTypes,
            List<string> originalWeaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,    
            List<GrammarMenuItem> grammarMenu,           
            List<QuestionTypeMenuItem> questionTypeMenu) 
            {
                if (string.IsNullOrEmpty(_apiKey)) return null;

            var reviewSection = weakTypeInfos.Any()
                ? string.Join("\n", weakTypeInfos
                    .Where(w => reviewTypes.Contains(w.QuestionTypeId))
                    .Select(w => $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})"))
                : "  - Không có dạng cần ôn lại.";

            var persistentSection = persistentFailTypes.Any()
                ? string.Join("\n", weakTypeInfos
                    .Where(w => persistentFailTypes.Contains(w.QuestionTypeId))
                    .Select(w => $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})"))
                : "  - Không có.";

            var filteredGrammarMenu = grammarMenu
                .Where(g => !persistentFailTypes.Contains(g.RelatedQuestionTypeId ?? ""))
                .ToList();

            var filteredQuizMenu = questionTypeMenu
                .Where(q => !persistentFailTypes.Contains(q.QuestionTypeId))
                .ToList();

            var grammarMenuJson = JsonSerializer.Serialize(
                filteredGrammarMenu.Select(g => new
                {
                    GrammarId = g.GrammarId,
                    Title = g.Title,
                    Syntaxes = g.Syntaxes,
                    Description = g.Description ?? "",
                    RelatedQuestionTypeId = g.RelatedQuestionTypeId ?? ""
                })
            );

            var quizMenuJson = JsonSerializer.Serialize(
                filteredQuizMenu.Select(q => new
                {
                    QuestionTypeId = q.QuestionTypeId,
                    Code = q.Code,
                    Name = q.Name,
                    Skill = q.Skill,
                    Description = q.Description ?? ""
                })
            );

            string strategy;
            string reviewInstruction;

            if (examScorePercent < 50)
            {
                strategy = "Học viên đạt dưới 50%. Tăng cường luyện tập, bố trí thêm VirtualQuiz.";
                reviewInstruction = reviewTypes.Any()
                    ? "Dành ngày 1 và ngày 2 để ÔN LẠI các dạng trong REVIEW LIST. Từ ngày 3 học nội dung mới."
                    : "Tập trung vào nội dung mới nhưng giảm tải độ khó.";
            }
            else if (examScorePercent < 80)
            {
                strategy = "Học viên đạt 50-79%. Ôn lại điểm yếu kết hợp học tiếp lộ trình.";
                reviewInstruction = reviewTypes.Any()
                    ? "Dành ngày 1 để ÔN LẠI REVIEW LIST. Từ ngày 2 học nội dung mới. Tỉ lệ ~20% ôn cũ / 80% mới."
                    : "Tiếp tục lộ trình bình thường.";
            }
            else
            {
                strategy = "Học viên đạt >= 80%. Tiến độ tốt, tăng tốc với kiến thức mới.";
                reviewInstruction = "Không cần ôn lại. Tập trung 100% nội dung mới, có thể tăng độ khó.";
            }

            var promptText = $@"
                Bạn là chuyên gia lập lộ trình TOPIK. Học viên bước vào TUẦN THỨ {nextWeekIndex}.
                Kết quả tuần trước: {examScorePercent}%.

                CHIẾN LƯỢC: {strategy}
                PHÂN BỔ NỘI DUNG: {reviewInstruction}

            *** DẠNG CẦN ÔN LẠI (REVIEW LIST - 20% nội dung tuần này) ***
                {reviewSection}

            *** DẠNG ĐÃ CẢNH BÁO 2 TUẦN - KHÔNG ĐƯA VÀO LỘ TRÌNH ***
                {persistentSection}

            *** QUY TẮC BẮT BUỘC ***
                1. CHỈ CHỌN từ MENU bên dưới. KHÔNG tự bịa.
                2. Task 'LearnTheory' → dùng GrammarId từ GRAMMAR MENU.
                   Ưu tiên grammar có RelatedQuestionTypeId trong REVIEW LIST.
                3. Task 'VirtualQuiz' → dùng QuestionTypeId từ QUIZ MENU.
                   Ưu tiên QuestionTypeId trong REVIEW LIST vào ngày đầu tuần.
                4. Ngày thứ 7 BẮT BUỘC là 'WeeklyExam' (Tiêu đề: Kiểm tra tuần {nextWeekIndex}).
                5. KHÔNG dùng bất kỳ dạng nào trong danh sách ĐÃ CẢNH BÁO.

            *** GRAMMAR MENU ***
                {grammarMenuJson}

            *** QUIZ MENU ***
                {quizMenuJson}

            *** OUTPUT JSON (chỉ 1 tuần, WeekIndex = {nextWeekIndex}) ***
                {{
                    ""Assessment"": ""Nhận xét ngắn gọn và lời khuyên cho tuần {nextWeekIndex}"",
                    ""Weeks"": [
                {{
                    ""WeekIndex"": {nextWeekIndex},
                    ""WeekGoal"": ""Mục tiêu tuần {nextWeekIndex}"",
                    ""Days"": [
                        {{
                            ""DayIndex"": 1,
                            ""Tasks"": [
                        {{
                      ""Title"": ""..."",
                      ""TaskType"": ""LearnTheory"",
                      ""Content"": ""..."",
                      ""GrammarId"": ""ID_TU_GRAMMAR_MENU""
                    }},
                    {{
                      ""Title"": ""..."",
                      ""TaskType"": ""VirtualQuiz"",
                      ""Content"": ""..."",
                      ""QuestionTypeId"": ""ID_TU_QUIZ_MENU""
                    }}
                  ]
                }}
              ]
            }}
          ]
        }}
    ";

            return await CallGeminiApiAsync(promptText);
        }
        private async Task<AiRoadmapResponse?> CallGeminiApiAsync(string promptText)
        {
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = promptText } } } } };
            try
            {
                var url = $"{BASE_URL}?key={_apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode) return null;

                using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Replace("```json", "").Replace("```", "").Trim();
                        int startIndex = text.IndexOf("{");
                        int endIndex = text.LastIndexOf("}");
                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            text = text.Substring(startIndex, endIndex - startIndex + 1);
                        }

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        return JsonSerializer.Deserialize<AiRoadmapResponse>(text, options);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API Error");
                return null;
            }
        }
        public async Task<string?> GenerateEntranceFeedbackAsync(
                TargetAimLevel targetAim,
                int readingWeakCount,
                int listeningWeakCount,
                int writingWeakCount,
                List<string> readingNames,
                List<string> listeningNames,
                List<string> writingNames,
                int recommendedDays)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            var readingSection = readingNames.Any()
                ? string.Join(", ", readingNames)
                : "không có";

            var listeningSection = listeningNames.Any()
                ? string.Join(", ", listeningNames)
                : "không có";

            var writingSection = writingNames.Any()
                ? string.Join(", ", writingNames)
                : "không có";

            var promptText = $@"
        Bạn là gia sư TOPIK thân thiện. Hãy viết nhận xét ngắn gọn (3-4 câu, bằng tiếng Việt) 
        cho học viên dựa trên kết quả bài test đầu vào.

        Thông tin kết quả:
        - Mục tiêu: {targetAim}
        - Phần Đọc: {readingWeakCount} dạng yếu ({readingSection})
        - Phần Nghe: {listeningWeakCount} dạng yếu ({listeningSection})
        - Phần Viết: {writingWeakCount} dạng yếu ({writingSection})
        - Lộ trình đề xuất: {recommendedDays} ngày

        Yêu cầu:
        1. Nhận xét thân thiện, động viên, không tiêu cực
        2. Chỉ ra skill nào yếu nhất cần tập trung
        3. Giải thích ngắn gọn tại sao cần {recommendedDays} ngày
        4. Kết thúc bằng lời khuyến khích

        Chỉ trả về đoạn text nhận xét, không có format hay markdown.
            ";

            var requestBody = new
            {
                contents = new[] {
            new { parts = new[] { new { text = promptText } } }
        }
            };

            try
            {
                var url = $"{BASE_URL}?key={_apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                if (!response.IsSuccessStatusCode) return null;

                using var jsonDoc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync());
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates)
                    && candidates.GetArrayLength() > 0)
                {
                    return candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()
                        ?.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi generate entrance feedback");
                return null;
            }
        }
    }
}