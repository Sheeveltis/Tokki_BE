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
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly string _apiKey;

        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public AiRoadmapService(
            HttpClient httpClient,
            ILogger<AiRoadmapService> logger,
            IConfiguration configuration,
            IKnowledgeBaseService knowledgeBaseService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AiSettings:ApiKey"]; 
            _knowledgeBaseService = knowledgeBaseService;
        }

        public async Task<AiRoadmapResponse?> GenerateStudyPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int durationDays,
            List<string> weaknesses)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("API Key chưa được cấu hình (AiSettings:ApiKey)");
                return null;
            }

            var weakContent = await _knowledgeBaseService.GetContentForWeaknessesAsync(weaknesses, currentLevel);
            var generalContent = await _knowledgeBaseService.GetGeneralContentForLevelAsync(currentLevel);

            var menuJson = JsonSerializer.Serialize(new
            {
                PriorityItems = weakContent.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type }), // Chỉ lấy trường cần thiết
                StandardItems = generalContent.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type })
            });

            var promptText = $@"
                Bạn là chuyên gia lập lộ trình TOPIK. Hãy tạo lộ trình {durationDays} ngày.
                - Mục tiêu: {target}
                - Trình độ hiện tại: {currentLevel}
                - Điểm yếu cần khắc phục: {(weaknesses != null && weaknesses.Any() ? string.Join(", ", weaknesses) : "Không có")}

                *** QUY TẮC BẮT BUỘC (QUAN TRỌNG): ***
                1. KHÔNG được tự bịa ra bài học. CHỈ ĐƯỢC CHỌN bài học từ danh sách 'MENU DỮ LIỆU' bên dưới.
                2. Nếu task là 'LearnTheory' -> Bắt buộc điền 'GrammarId' lấy từ Menu (TargetId của Type=1).
                3. Nếu task là 'VirtualQuiz' -> Bắt buộc điền 'QuestionTypeId' lấy từ Menu (TargetId của Type=2).
                4. Ngày thứ 7 hàng tuần phải có task 'WeeklyExam' (Title: 'Thi thử tuần').
                5. Ưu tiên xếp 'PriorityItems' (Điểm yếu) vào các ngày đầu tiên.

                *** MENU DỮ LIỆU (CHỈ CHỌN TRONG NÀY): ***
                {menuJson}

                *** ĐỊNH DẠNG OUTPUT (JSON ONLY): ***
                Trả về JSON thuần (không markdown), cấu trúc:
                {{
                  ""Assessment"": ""Nhận xét về trình độ và điểm yếu"",
                  ""Weeks"": [
                    {{
                      ""WeekIndex"": 1,
                      ""WeekGoal"": ""Mục tiêu tuần 1"",
                      ""Days"": [
                        {{
                          ""DayIndex"": 1,
                          ""Tasks"": [
                             {{ 
                               ""Title"": ""Tên bài hiển thị"", 
                               ""TaskType"": ""LearnTheory"", 
                               ""Content"": ""Lời khuyên ngắn"",
                               ""GrammarId"": ""ID_TU_MENU"" 
                             }},
                             {{ 
                               ""Title"": ""Luyện tập"", 
                               ""TaskType"": ""VirtualQuiz"", 
                               ""Content"": ""Mô tả bài tập"",
                               ""QuestionTypeId"": ""ID_TU_MENU"" 
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

                using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Replace("```json", "").Replace("```", "").Trim();

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
            List<string> originalWeaknesses)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            var reviewContent = reviewTypes.Any()
                ? await _knowledgeBaseService.GetContentForWeaknessesAsync(reviewTypes, currentLevel)
                : new List<KnowledgeMetadata>();

            var generalContent = await _knowledgeBaseService.GetGeneralContentForLevelAsync(currentLevel);
            var filteredNew = generalContent
                .Where(x => !reviewTypes.Contains(x.TargetId) && !persistentFailTypes.Contains(x.TargetId))
                .ToList();

            var menuJson = JsonSerializer.Serialize(new
            {
                ReviewItems = reviewContent.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type }),
                NewItems = filteredNew.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type })
            });

            string strategy;
            string reviewInstruction;

            if (examScorePercent < 50)
            {
                strategy = "Học viên đạt dưới 50%. Tăng cường luyện tập, bố trí thêm VirtualQuiz.";
                reviewInstruction = reviewTypes.Any()
                    ? $"Dành ngày 1 và ngày 2 để ÔN LẠI các dạng trong ReviewItems ({string.Join(", ", reviewTypes)}). Từ ngày 3 học NewItems."
                    : "Tập trung vào các dạng mới nhưng giảm tải độ khó.";
            }
            else if (examScorePercent < 80)
            {
                strategy = "Học viên đạt 50-79%. Ôn lại điểm yếu kết hợp học tiếp lộ trình.";
                reviewInstruction = reviewTypes.Any()
                    ? $"Dành ngày 1 để ÔN LẠI ReviewItems ({string.Join(", ", reviewTypes)}). Từ ngày 2 học NewItems. Tỉ lệ: ~20% ôn cũ, 80% mới."
                    : "Tiếp tục lộ trình bình thường.";
            }
            else
            {
                strategy = "Học viên đạt >= 80%. Tiến độ tốt, tăng tốc với kiến thức mới.";
                reviewInstruction = "Không cần ôn lại. Tập trung 100% vào NewItems, có thể tăng độ khó.";
            }

            string persistentNote = persistentFailTypes.Any()
                ? $"KHÔNG được dùng các dạng sau (user đã được ôn 2 tuần liên tiếp nhưng vẫn chưa pass, hệ thống đã cảnh báo và sẽ bỏ qua): {string.Join(", ", persistentFailTypes)}."
                : string.Empty;

            var promptText = $@"
                Bạn là chuyên gia lập lộ trình TOPIK. Học viên chuẩn bị bước vào TUẦN THỨ {nextWeekIndex}.
                Kết quả tuần trước: {examScorePercent}%.
        
                CHIẾN LƯỢC: {strategy}
                PHÂN BỔ NỘI DUNG: {reviewInstruction}
            {persistentNote}

                *** QUY TẮC BẮT BUỘC ***
            1. CHỈ CHỌN bài học từ MENU DỮ LIỆU bên dưới. KHÔNG tự bịa.
            2. Ngày thứ 7 BẮT BUỘC là 'WeeklyExam' (Tiêu đề: Kiểm tra định kỳ tuần {nextWeekIndex}).
            3. Nếu có ReviewItems: ưu tiên xếp vào ngày đầu tuần.
            4. Tỉ lệ ReviewItems / NewItems xấp xỉ 20% / 80% (tính theo số task, không phải số ngày).
            5. Nếu ReviewItems rỗng: dùng 100% NewItems.

        *** MENU DỮ LIỆU ***
        {menuJson}

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
                    {{ ""Title"": ""..."", ""TaskType"": ""LearnTheory"", ""Content"": ""..."", ""GrammarId"": ""ID_TU_MENU"" }},
                    {{ ""Title"": ""..."", ""TaskType"": ""VirtualQuiz"", ""Content"": ""..."", ""QuestionTypeId"": ""ID_TU_MENU"" }}
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
    }
}