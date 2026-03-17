using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
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
        private readonly bool _useVertex;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _credentialsPath;
        private readonly string _modelName;

        public AiRoadmapService(
            HttpClient httpClient,
            ILogger<AiRoadmapService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AiSettings:ApiKey"] ?? "";
            _useVertex = bool.TryParse(configuration["AiSettings:UseVertex"], out var uv) && uv;
            _projectId = configuration["AiSettings:VertexProjectId"] ?? "";
            _location = configuration["AiSettings:VertexLocation"] ?? "us-central1";
            _credentialsPath = configuration["AiSettings:VertexCredentialsPath"] ?? "";
            _modelName = configuration["AiSettings:ModelName"] ?? "gemini-2.5-flash";
        }
        private string GetApiUrl()
        {
            if (_useVertex)
                return $"https://{_location}-aiplatform.googleapis.com/v1/projects/{_projectId}" +
                       $"/locations/{_location}/publishers/google/models/{_modelName}:generateContent";

            return $"https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent?key={_apiKey}";
        }

        private bool IsConfigValid()
        {
            if (_useVertex)
            {
                if (string.IsNullOrEmpty(_credentialsPath))
                {
                    _logger.LogError("Vertex AI: VertexCredentialsPath chưa được cấu hình.");
                    return false;
                }
                if (!File.Exists(_credentialsPath))
                {
                    _logger.LogError($"Vertex AI: Không tìm thấy file credentials tại '{_credentialsPath}'.");
                    return false;
                }
                return true;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Gemini API: ApiKey chưa được cấu hình.");
                return false;
            }
            return true;
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var credential = GoogleCredential
                    .FromFile(_credentialsPath)
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                return await credential.UnderlyingCredential
                    .GetAccessTokenForRequestAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy access token Vertex AI");
                return null;
            }
        }

        private async Task<bool> PrepareHttpClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!_useVertex) return true;

            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return true;
        }
        private async Task<AiRoadmapResponse?> CallGeminiApiAsync(string promptText)
        {
            if (!IsConfigValid()) return null;

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = promptText } } } }
            };

            try
            {
                var ready = await PrepareHttpClientAsync();
                if (!ready) return null;

                var url = GetApiUrl();
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Lỗi gọi AI ({response.StatusCode}): {err}");
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
        private async Task<string?> CallGeminiTextAsync(string promptText)
        {
            if (!IsConfigValid()) return null;

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = promptText } } } }
            };

            try
            {
                var ready = await PrepareHttpClientAsync();
                if (!ready) return null;

                var url = GetApiUrl();
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
                _logger.LogError(ex, "Exception khi gọi AI Service (text)");
                return null;
            }
        }
        public async Task<AiRoadmapResponse?> GenerateStudyPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int durationDays,
            List<string> weaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,
            List<QuestionTypeMenuItem> questionTypeMenu,
            int typesPerWeek,
            int totalWeeks)
        {
            var weaknessSection = weakTypeInfos.Any()
                ? string.Join("\n", weakTypeInfos.Select(w =>
                    $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})" +
                    (string.IsNullOrEmpty(w.Description) ? "" : $" — {w.Description}")))
                : "  - Không có điểm yếu cụ thể, học theo lộ trình chuẩn.";

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
                Bạn là chuyên gia lập lộ trình TOPIK. Hãy tạo nội dung cho TUẦN ĐẦU TIÊN.
                - Mục tiêu: {target}
                - Trình độ hiện tại: {GetLevelDescription(currentLevel)}
                - Tổng lộ trình: {totalWeeks} tuần

            *** ĐIỂM YẾU CỦA HỌC VIÊN (BẮT BUỘC ƯU TIÊN) ***
            {weaknessSection}

            *** PHÂN BỔ NỘI DUNG TUẦN NÀY ***
            - Tuần này chỉ học TỐI ĐA {typesPerWeek} dạng câu từ danh sách ĐIỂM YẾU ở trên
            - Mỗi ngày tối đa 3-4 task, KHÔNG được nhồi nhét quá nhiều
            - Mỗi dạng câu nên có CẢ LearnTheory lẫn VirtualQuiz:
            + LearnTheory: học lý thuyết về dạng đó trước
            + VirtualQuiz: luyện tập thực hành sau khi học lý thuyết
            - Phân bổ đều vào 6 ngày đầu (ngày 7 là WeeklyExam)

            *** QUY TẮC BẮT BUỘC ***
                1. CHỈ CHỌN QuestionTypeId từ MENU bên dưới. KHÔNG tự bịa.
                2. Task 'LearnTheory':
                - Điền 'QuestionTypeId' của dạng đang học (lấy từ MENU)
                - Điền 'Content' là nội dung HTML lý thuyết đầy đủ gồm:
                + Giải thích dạng câu hỏi này yêu cầu kỹ năng gì
                + Các pattern/cấu trúc ngữ pháp thường gặp (nếu có)
                + 2-3 ví dụ minh họa bằng tiếng Hàn (có dịch)
                + Mẹo làm bài nhanh
                - Format HTML: <h3>...</h3><p>...</p><ul><li>...</li></ul>
                - KHÔNG điền GrammarId
                3. Task 'VirtualQuiz':
                - Điền 'QuestionTypeId' của dạng cần luyện tập (lấy từ MENU)
                - Điền 'Content' là lời khuyên ngắn gọn
                4. Ngày thứ 7 BẮT BUỘC là 'WeeklyExam' (Title: 'Thi thử tuần').

            *** QUIZ MENU (dùng cho cả LearnTheory và VirtualQuiz) ***
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
                      ""Title"": ""Lý thuyết: Tên dạng bài"",
                      ""TaskType"": ""LearnTheory"",
                      ""Content"": ""<h3>...</h3><p>...</p>"",
                      ""QuestionTypeId"": ""ID_TU_MENU"",
                      ""GrammarId"": null
                    }},
                    {{
                      ""Title"": ""Luyện tập: Tên dạng bài"",
                      ""TaskType"": ""VirtualQuiz"",
                      ""Content"": ""Lời khuyên ngắn"",
                      ""QuestionTypeId"": ""ID_TU_MENU"",
                      ""GrammarId"": null
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
        public async Task<AiRoadmapResponse?> GenerateNextWeekPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int nextWeekIndex,
            int examScorePercent,
            List<string> reviewTypes,
            List<string> persistentFailTypes,
            List<string> originalWeaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,
            List<QuestionTypeMenuItem> questionTypeMenu)
        {
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

            var filteredQuizMenu = questionTypeMenu
                .Where(q => !persistentFailTypes.Contains(q.QuestionTypeId))
                .ToList();

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
                strategy = "Học viên đạt dưới 50%. Tăng cường luyện tập.";
                reviewInstruction = reviewTypes.Any()
                    ? "Dành ngày 1 và ngày 2 để ÔN LẠI các dạng trong REVIEW LIST. Từ ngày 3 học mới."
                    : "Tập trung nội dung mới nhưng giảm tải độ khó.";
            }
            else if (examScorePercent < 80)
            {
                strategy = "Học viên đạt 50-79%. Kết hợp ôn cũ và học mới.";
                reviewInstruction = reviewTypes.Any()
                    ? "Dành ngày 1 ôn REVIEW LIST (~20%). Từ ngày 2 học mới (80%)."
                    : "Tiếp tục lộ trình bình thường.";
            }
            else
            {
                strategy = "Học viên đạt >= 80%. Tăng tốc với kiến thức mới.";
                reviewInstruction = "Không cần ôn lại. Tập trung 100% nội dung mới.";
            }

            var promptText = $@"
            Bạn là chuyên gia lập lộ trình TOPIK. Học viên bước vào TUẦN THỨ {nextWeekIndex}.
            Kết quả tuần trước: {examScorePercent}%.
            Trình độ hiện tại: {GetLevelDescription(currentLevel)}
            CHIẾN LƯỢC: {strategy}
            PHÂN BỔ: {reviewInstruction}

            *** DẠNG CẦN ÔN LẠI (REVIEW LIST - 20%) ***
            {reviewSection}

            *** DẠNG ĐÃ CẢNH BÁO 2 TUẦN - KHÔNG ĐƯA VÀO ***
            {persistentSection}

            *** QUY TẮC BẮT BUỘC ***
            1. CHỈ CHỌN từ MENU. KHÔNG tự bịa.
            2. Task 'LearnTheory':
           - Điền 'QuestionTypeId' của dạng đang học
           - Điền 'Content' là HTML lý thuyết đầy đủ (định nghĩa, pattern, ví dụ, mẹo)
           - KHÔNG điền GrammarId
            3. Task 'VirtualQuiz': Điền 'QuestionTypeId', Content ngắn gọn
            4. Ngày 7 BẮT BUỘC là 'WeeklyExam'
            5. Mỗi ngày tối đa 3-4 task
            6. KHÔNG dùng dạng trong danh sách ĐÃ CẢNH BÁO

            *** QUIZ MENU ***
            {quizMenuJson}

        *** OUTPUT JSON (WeekIndex = {nextWeekIndex}) ***
        {{
          ""Assessment"": ""Nhận xét và lời khuyên tuần {nextWeekIndex}"",
          ""Weeks"": [ {{ ... }} ]
        }}
    ";
            return await CallGeminiApiAsync(promptText);
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
            var readingSection = readingNames.Any()
                ? string.Join(", ", readingNames) : "không có";

            var listeningSection = listeningNames.Any()
                ? string.Join(", ", listeningNames) : "không có";

            var writingSection = writingNames.Any()
                ? string.Join(", ", writingNames) : "không có";

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

            return await CallGeminiTextAsync(promptText);
        }
        private static string GetLevelDescription(CurrentTopikLevel level) => level switch
        {
            CurrentTopikLevel.Pre_Topik => "Chưa có nền tảng TOPIK, cần học từ cơ bản",
            CurrentTopikLevel.Level_1 => "TOPIK I Level 1 (A1) — nền tảng cơ bản",
            CurrentTopikLevel.Level_2 => "TOPIK I Level 2 (A2) — sơ cấp",
            CurrentTopikLevel.Pre_Topik_II => "Đang xây dựng nền tảng TOPIK II — chưa đạt Level 3, cần học chắc kiến thức trung cấp trước",
            CurrentTopikLevel.Level_3 => "TOPIK II Level 3 (B1) — trung cấp",
            CurrentTopikLevel.Level_4 => "TOPIK II Level 4 (B2) — trung cấp cao",
            CurrentTopikLevel.Level_5 => "TOPIK II Level 5 (C1) — cao cấp",
            CurrentTopikLevel.Level_6 => "TOPIK II Level 6 (C2) — thành thạo",
            _ => level.ToString()
        };
    }
}