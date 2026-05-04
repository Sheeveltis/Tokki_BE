using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Tokki.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Constants;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class AiRoadmapService : IAiRoadmapService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiRoadmapService> _logger;
        private readonly GeminiOptions _geminiOptions;
        private readonly ISystemConfigRepository _configRepo;

        public AiRoadmapService(
            HttpClient httpClient,
            ILogger<AiRoadmapService> logger,
            IOptions<GeminiOptions> geminiOptions,
            ISystemConfigRepository configRepo)
        {
            _httpClient = httpClient;
            _logger = logger;
            _geminiOptions = geminiOptions.Value;
            _configRepo = configRepo;
        }

        private async Task<int> GetIntConfigAsync(string key, int fallback)
        {
            try
            {
                var cfg = await _configRepo.GetByKeyAsync(key);
                if (cfg is { IsActive: true, Value: not null }
                    && int.TryParse(cfg.Value, out int parsed))
                    return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không đọc được config '{Key}', dùng fallback={Fallback}.", key, fallback);
            }
            return fallback;
        }

        private string GetApiUrl()
        {
            var config = _geminiOptions.Roadmap;
            string baseUrl = config.BaseUrl?.TrimEnd('/') ?? string.Empty;

            if (_geminiOptions.UseVertex)
            {
                if (!string.IsNullOrEmpty(_geminiOptions.VertexProjectId))
                {
                    return $"{baseUrl}/projects/{_geminiOptions.VertexProjectId}" +
                           $"/locations/{_geminiOptions.VertexLocation}/publishers/google/models/{config.Model}:generateContent";
                }
                return $"{baseUrl}/models/{config.Model}:generateContent";
            }

            return $"{baseUrl}/models/{config.Model}:generateContent?key={config.ApiKey}";
        }

        private bool IsConfigValid()
        {
            var config = _geminiOptions.Roadmap;

            if (string.IsNullOrEmpty(config.BaseUrl) || string.IsNullOrEmpty(config.Model))
            {
                _logger.LogError("Gemini: Thiếu cấu hình Roadmap (BaseUrl hoặc Model).");
                return false;
            }

            if (_geminiOptions.UseVertex)
            {
                if (string.IsNullOrEmpty(_geminiOptions.VertexCredentialsPath))
                {
                    _logger.LogError("Vertex AI: VertexCredentialsPath chưa được cấu hình.");
                    return false;
                }
                if (!File.Exists(_geminiOptions.VertexCredentialsPath))
                {
                    _logger.LogError($"Vertex AI: Không tìm thấy file credentials tại '{_geminiOptions.VertexCredentialsPath}'.");
                    return false;
                }
                return true;
            }

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                _logger.LogError("Gemini API: Thiếu ApiKey cho Roadmap.");
                return false;
            }
            return true;
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var credential = GoogleCredential
                    .FromFile(_geminiOptions.VertexCredentialsPath)
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

            if (!_geminiOptions.UseVertex) return true;

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
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = promptText } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    maxOutputTokens = 8192,
                    temperature = 0.3
                }
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

                if (root.TryGetProperty("usageMetadata", out var usage))
                {
                    var inputTokens = usage.TryGetProperty("promptTokenCount", out var i) ? i.GetInt32() : 0;
                    var outputTokens = usage.TryGetProperty("candidatesTokenCount", out var o) ? o.GetInt32() : 0;
                    var totalTokens = usage.TryGetProperty("totalTokenCount", out var t) ? t.GetInt32() : 0;
                    _logger.LogInformation(
                        "[Gemini Token Usage - Roadmap JSON] Input: {InputTokens} | Output: {OutputTokens} | Total: {TotalTokens}",
                        inputTokens, outputTokens, totalTokens);
                }

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
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = promptText } }
                    }
                }
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

                if (root.TryGetProperty("usageMetadata", out var usage))
                {
                    var inputTokens = usage.TryGetProperty("promptTokenCount", out var i) ? i.GetInt32() : 0;
                    var outputTokens = usage.TryGetProperty("candidatesTokenCount", out var o) ? o.GetInt32() : 0;
                    var totalTokens = usage.TryGetProperty("totalTokenCount", out var t) ? t.GetInt32() : 0;
                    _logger.LogInformation(
                        "[Gemini Token Usage - Text] Input: {InputTokens} | Output: {OutputTokens} | Total: {TotalTokens}",
                        inputTokens, outputTokens, totalTokens);
                }

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

        public async Task<List<string>> SequenceWeaknessesAsync(
            List<string> questionTypeIds,
            CurrentTopikLevel currentLevel,
            TargetAimLevel targetLevel,
            List<QuestionTypeMenuItem> typeMenu,
            CancellationToken token = default)
        {
            _logger.LogInformation("Sắp xếp trình tự cho {Count} điểm yếu.", questionTypeIds.Count);

            var menuDetails = string.Join("\n", typeMenu.Select(m =>
                $"- ID: {m.QuestionTypeId}, Mã: {m.Code}, Tên: {m.Name}, Kỹ năng: {m.Skill}"));

            string prompt = $@"
            Bạn là chuyên gia sư phạm TOPIK. Sắp xếp danh sách QuestionTypeIds sau đây thành một trình tự học tập khoa học nhất:
            THÔNG TIN:
            - Trình độ: {currentLevel}, Mục tiêu: {targetLevel}.
            - Danh sách điểm yếu: {menuDetails}

            QUY TẮC SẮP XẾP (ƯU TIÊN):
            1. SKILL INTERLEAVING: Xen kẽ các kỹ năng (Nghe, Đọc, Viết). Không để học viên phải học quá 2 ngày liên tiếp cùng một kỹ năng để tránh gây nhàm chán và mệt mỏi cho não bộ.
            2. COMPLEXITY: Đưa các dạng bài dễ hoặc nền tảng lên trước, các dạng bài phức tạp (như Viết đoạn văn) nên xen kẽ vào giữa.
            3. FIFO BASE: Vẫn giữ nền tảng là các câu sai trước nhưng có thể điều chỉnh vị trí để thỏa mãn quy tắc xen kẽ kỹ năng.

            KẾT QUẢ: Chỉ trả về danh sách ID phân cách bằng dấu phẩy.
            Ví dụ: ID1, ID2, ID3...";

            var result = await CallGeminiTextAsync(prompt);
            if (string.IsNullOrEmpty(result)) return questionTypeIds;

            var cleanedResult = result.Split(',')
                .Select(s => s.Trim())
                .Where(s => questionTypeIds.Contains(s))
                .ToList();

            return cleanedResult.Any() ? cleanedResult : questionTypeIds;
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
            int maxTasksPerDay = await GetIntConfigAsync(PromptConfigKeys.MaxTasksPerDay, fallback: 4);
            int studyDays = await GetIntConfigAsync(PromptConfigKeys.StudyDaysPerWeek, fallback: 6);
            int weeklyExamDay = await GetIntConfigAsync(PromptConfigKeys.WeeklyExamDay, fallback: 7);

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
            - Mỗi ngày tối đa {maxTasksPerDay} task, KHÔNG được nhồi nhét quá nhiều
            - Mỗi dạng câu nên có CẢ LearnTheory lẫn VirtualQuiz:
            + LearnTheory: học lý thuyết về dạng đó trước
            + VirtualQuiz: luyện tập thực hành sau khi học lý thuyết
            - Phân bổ đều vào {studyDays} ngày đầu (ngày {weeklyExamDay} là WeeklyExam)

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
                4. KHÔNG tạo task WeeklyExam — bài kiểm tra sẽ được hệ thống tự động tạo ở ngày thứ {weeklyExamDay}.

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
            int totalWeeks,
            List<string> focusTypeIds,
            List<string> deferredTypeIds,
            List<QuestionTypeMenuItem> weakTypeInfos,
            List<QuestionTypeMenuItem> fullMenu)
        {
            int maxTasksPerDay = await GetIntConfigAsync(PromptConfigKeys.MaxTasksPerDay, fallback: 4);
            int studyDays = await GetIntConfigAsync(PromptConfigKeys.StudyDaysPerWeek, fallback: 6);
            int weeklyExamDay = await GetIntConfigAsync(PromptConfigKeys.WeeklyExamDay, fallback: 7);

            var focusDetails = string.Join("\n", weakTypeInfos
                .Where(w => focusTypeIds.Contains(w.QuestionTypeId))
                .Select(w => $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})" +
                             (string.IsNullOrEmpty(w.Description) ? "" : $" — {w.Description}")));

            var deferredSection = deferredTypeIds.Any()
                ? "DẠNG ĐÃ FAIL 2 TUẦN LIÊN TIẾP - TẠM HOÃN (KHÔNG đưa vào tuần này):\n" +
                  string.Join("\n", fullMenu
                      .Where(q => deferredTypeIds.Contains(q.QuestionTypeId))
                      .Select(q => $"  - [{q.Skill}] {q.Name}"))
                : "Không có dạng bài tạm hoãn.";

            var filteredMenu = fullMenu
                .Where(q => !deferredTypeIds.Contains(q.QuestionTypeId))
                .ToList();

            var quizMenuJson = JsonSerializer.Serialize(
                filteredMenu.Select(q => new
                {
                    QuestionTypeId = q.QuestionTypeId,
                    Code = q.Code,
                    Name = q.Name,
                    Skill = q.Skill,
                    Description = q.Description ?? ""
                })
            );

            var promptText = $@"
            Bạn là chuyên gia thiết kế lộ trình học TOPIK cấp độ {target}.
            Hãy thiết kế nội dung học tập TUYỆT VỜI cho TUẦN {nextWeekIndex}/{totalWeeks}.

            THÔNG TIN HỌC VIÊN:
            - Mục tiêu: {target}
            - Trình độ hiện tại: {GetLevelDescription(currentLevel)}
            - Dạng bài trọng tâm tuần này (theo thứ tự ưu tiên FIFO):
            {focusDetails}
            - {deferredSection}

            QUY TẮC THIẾT KẾ & PHÂN BỔ (QUAN TRỌNG):
            1. PHÂN BỔ DẠNG BÀI (TUÂN THỦ FIFO): Các dạng bài trọng tâm được đánh số theo thứ tự ưu tiên. Bạn PHẢI phân bổ như sau:
               - THỨ 2 (DayIndex 1): Dạng bài 1 (đầu tiên trong danh sách).
               - THỨ 3 (DayIndex 2): Dạng bài 2.
               - THỨ 4 (DayIndex 3): Dạng bài 3.
               - THỨ 5 (DayIndex 4): Dạng bài 4.
               - THỨ 6 (DayIndex 5): Dạng bài 5.
            2. CHI TIẾT CÁC NGÀY:
               - THỨ 2 (DayIndex 1):
                 * BẮT BUỘC có 1 task ""Document"" đầu tiên (questionTypeId: null).
                   Tiêu đề: ""Tái khởi động và Phân tích nội dung tuần {nextWeekIndex}"".
                   Nội dung (HTML): Tóm tắt tiến độ tuần trước. Nếu có dạng tạm hoãn, giải thích
                   khéo léo rằng các dạng này tạm dừng để tập trung dạng mới, tránh quá tải.
                   Cuối cùng nêu bật mục tiêu 5 dạng bài tuần này.
                 * Sau đó 2 task cho Dạng bài 1: LearnTheory và VirtualQuiz.
               - THỨ 3 - THỨ 6 (DayIndex 2-5): Mỗi ngày 1 dạng bài duy nhất. Mỗi ngày 2 task: LearnTheory + VirtualQuiz.
               - THỨ 7 (TỔNG ÔN TẬP - DayIndex 6):
                 * 1 Task LearnTheory (questionTypeId: null): Tiêu đề BẮT BUỘC ""Tóm tắt tinh hoa và chiến thuật các dạng bài tuần {nextWeekIndex}"".
                 * Các Task VirtualQuiz: Mỗi task tương ứng 1 QuestionTypeId khác nhau từ 5 dạng tuần này.
               - CHỦ NHẬT (THI THỬ - DayIndex 7):
                 * 1 Task WeeklyExam (questionTypeId: null): Tiêu đề ""Bài kiểm tra tổng hợp tuần {nextWeekIndex}"".
            3. QUY ĐỊNH VỀ ID:
               - CHỈ sử dụng QuestionTypeId từ danh sách dạng bài trọng tâm đã cho.
               - Task ""Document"", ""WeeklyExam"" và LearnTheory của DayIndex 6 BẮT BUỘC để ""questionTypeId"": null.
               - KHÔNG tự sinh ID mới, KHÔNG dùng ID không có trong danh sách.
               - KHÔNG dùng dạng bài trong danh sách TẠM HOÃN.

            YÊU CẦU NỘI DUNG CHO TASK LearnTheory (CỰC KỲ QUAN TRỌNG):
            Phần ""Content"" của task LearnTheory phải dài, chi tiết, trình bày bằng HTML đẹp mắt, chia 2 phần lớn:
            1. GIỚI THIỆU:
               - Mô tả chi tiết dạng bài: cấu trúc, yêu cầu kỹ năng, những gì cần tập trung (Key focus).
               - Phương pháp học và làm bài hiệu quả (Tips & Strategies).
            2. LÝ THUYẾT:
               - Kiến thức chuyên sâu, điểm lưu ý quan trọng, lỗi sai thường gặp.
               - Ngữ pháp thông dụng thường xuất hiện trong dạng bài này.
               - 2-3 ví dụ minh họa bằng tiếng Hàn (có dịch).
               - Kiến thức bổ trợ hoặc nâng cao trình độ.
            Format HTML: <h3>...</h3><p>...</p><ul><li>...</li></ul>

            CỐ ĐỊNH CÁC LOẠI TASK: LearnTheory, VirtualQuiz, WeeklyExam, Document.

            *** QUIZ MENU (chỉ dùng ID từ đây) ***
            {quizMenuJson}

            *** OUTPUT JSON (WeekIndex = {nextWeekIndex}) ***
            {{
              ""Assessment"": ""Nhận xét chuyên sâu tuần {nextWeekIndex}..."",
              ""Weeks"": [
                {{
                  ""WeekIndex"": {nextWeekIndex},
                  ""WeekGoal"": ""Mục tiêu đạt được sau tuần này"",
                  ""Days"": [
                    {{
                      ""DayIndex"": 1,
                      ""Tasks"": [
                        {{
                          ""Title"": ""Tái khởi động và Phân tích nội dung tuần {nextWeekIndex}"",
                          ""TaskType"": ""Document"",
                          ""QuestionTypeId"": null,
                          ""Content"": ""HTML tóm tắt..."",
                          ""GrammarId"": null
                        }},
                        {{
                          ""Title"": ""Lý thuyết: Tên dạng bài 1"",
                          ""TaskType"": ""LearnTheory"",
                          ""QuestionTypeId"": ""ID_TU_MENU"",
                          ""Content"": ""<h3>Giới thiệu</h3>...<h3>Lý thuyết</h3>..."",
                          ""GrammarId"": null
                        }},
                        {{
                          ""Title"": ""Luyện tập: Tên dạng bài 1"",
                          ""TaskType"": ""VirtualQuiz"",
                          ""QuestionTypeId"": ""ID_TU_MENU"",
                          ""Content"": ""Lời khuyên ngắn"",
                          ""GrammarId"": null
                        }}
                      ]
                    }},
                    {{
                      ""DayIndex"": 6,
                      ""Tasks"": [
                        {{
                          ""Title"": ""Tóm tắt tinh hoa và chiến thuật các dạng bài tuần {nextWeekIndex}"",
                          ""TaskType"": ""LearnTheory"",
                          ""QuestionTypeId"": null,
                          ""Content"": ""HTML tổng ôn..."",
                          ""GrammarId"": null
                        }}
                      ]
                    }},
                    {{
                      ""DayIndex"": 7,
                      ""Tasks"": [
                        {{
                          ""Title"": ""Bài kiểm tra tổng hợp tuần {nextWeekIndex}"",
                          ""TaskType"": ""WeeklyExam"",
                          ""QuestionTypeId"": null,
                          ""Content"": null,
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
        public async Task<AiRoadmapResponse?> GenerateExpansionWeekPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int nextWeekIndex,
            List<string> expansionTypes,
            List<string> originalWeaknessTypeIds,
            List<QuestionTypeMenuItem> expansionTypeInfos,
            List<QuestionTypeMenuItem> fullMenu)
        {
            int maxTasksPerDay = await GetIntConfigAsync(PromptConfigKeys.MaxTasksPerDay, fallback: 4);
            int studyDays = await GetIntConfigAsync(PromptConfigKeys.StudyDaysPerWeek, fallback: 6);
            int weeklyExamDay = await GetIntConfigAsync(PromptConfigKeys.WeeklyExamDay, fallback: 7);

            var expansionSection = expansionTypeInfos.Any()
                ? string.Join("\n", expansionTypeInfos.Select(w =>
                    $"  - [{w.Skill}] {w.Name} (QuestionTypeId: {w.QuestionTypeId})" +
                    (string.IsNullOrEmpty(w.Description) ? "" : $" — {w.Description}")))
                : "  - Không có dạng bài mới.";

            var originalWeaknessSection = originalWeaknessTypeIds.Any()
                ? string.Join(", ", fullMenu
                    .Where(q => originalWeaknessTypeIds.Contains(q.QuestionTypeId))
                    .Select(q => q.Name))
                : "Không có";

            var quizMenuJson = JsonSerializer.Serialize(
                expansionTypeInfos.Select(q => new
                {
                    QuestionTypeId = q.QuestionTypeId,
                    Code = q.Code,
                    Name = q.Name,
                    Skill = q.Skill,
                    Description = q.Description ?? ""
                })
            );

            var promptText = $@"
            Bạn là chuyên gia lập lộ trình TOPIK. Học viên bước vào TUẦN MỞ RỘNG {nextWeekIndex}.

            THÔNG TIN HỌC VIÊN:
            - Mục tiêu: {target}
            - Trình độ hiện tại: {GetLevelDescription(currentLevel)}
            - Học viên đã HOÀN THÀNH và PASS toàn bộ các dạng yếu ban đầu gồm: {originalWeaknessSection}
            - Tuần này sẽ GIỚI THIỆU thêm các dạng bài mới để mở rộng kiến thức toàn diện cho kỳ thi TOPIK

            *** DẠNG BÀI MỚI CẦN GIỚI THIỆU TUẦN NÀY ***
            {expansionSection}

            *** PHÂN BỔ NỘI DUNG ***
            - Mỗi ngày học 1 dạng bài mới (LearnTheory + VirtualQuiz)
            - Phân bổ đều vào {studyDays} ngày đầu, mỗi ngày tối đa {maxTasksPerDay} task
            - Ngày {weeklyExamDay} KHÔNG có WeeklyExam (đề thi cuối tuần mở rộng sẽ được tạo tự động từ danh sách dạng yếu ban đầu)

            *** QUY TẮC BẮT BUỘC ***
            1. CHỈ CHỌN QuestionTypeId từ MENU bên dưới. KHÔNG tự bịa.
            2. Task 'LearnTheory':
               - Điền 'QuestionTypeId' của dạng đang giới thiệu (lấy từ MENU)
               - Điền 'Content' là HTML lý thuyết đầy đủ:
                 + Mô tả dạng câu hỏi, yêu cầu kỹ năng
                 + Các pattern/cấu trúc ngữ pháp thường gặp
                 + 2-3 ví dụ minh họa bằng tiếng Hàn (có dịch)
                 + Mẹo làm bài nhanh
               - Format HTML: <h3>...</h3><p>...</p><ul><li>...</li></ul>
               - Tone: giới thiệu lần đầu, thân thiện, khuyến khích khám phá
            3. Task 'VirtualQuiz':
               - Điền 'QuestionTypeId' của dạng cần luyện tập (lấy từ MENU)
               - Điền 'Content' là lời khuyên ngắn gọn về cách tiếp cận dạng bài này
            4. KHÔNG tạo task WeeklyExam — bài kiểm tra sẽ được tạo tự động
            5. Assessment: Viết lời động viên vì học viên đã hoàn thành giai đoạn ôn luyện điểm yếu

            *** QUIZ MENU ***
            {quizMenuJson}

            *** OUTPUT JSON (WeekIndex = {nextWeekIndex}) ***
            {{
              ""Assessment"": ""Chúc mừng! Học viên đã hoàn thành giai đoạn ôn luyện. Tuần này chúng ta sẽ khám phá thêm..."",
              ""Weeks"": [
                {{
                  ""WeekIndex"": {nextWeekIndex},
                  ""WeekGoal"": ""Mục tiêu mở rộng tuần này"",
                  ""Days"": [
                    {{
                      ""DayIndex"": 1,
                      ""Tasks"": [
                        {{
                          ""Title"": ""Khám phá: Tên dạng bài"",
                          ""TaskType"": ""LearnTheory"",
                          ""Content"": ""<h3>...</h3><p>...</p>"",
                          ""QuestionTypeId"": ""ID_TU_MENU"",
                          ""GrammarId"": null
                        }},
                        {{
                          ""Title"": ""Thử sức: Tên dạng bài"",
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