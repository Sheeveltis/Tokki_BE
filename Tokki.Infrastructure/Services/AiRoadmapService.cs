using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs; 
using Tokki.Domain.Enums;

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
            int nextWeekIndex,
            int examScore,
            List<string> detectedWeaknesses,
            List<string> originalWeaknesses)
        {
            if (string.IsNullOrEmpty(_apiKey)) return null;

            var weakContent = await _knowledgeBaseService.GetContentForWeaknessesAsync(detectedWeaknesses, CurrentTopikLevel.Level_1); 
            var generalContent = await _knowledgeBaseService.GetGeneralContentForLevelAsync(CurrentTopikLevel.Level_1);

            var menuJson = JsonSerializer.Serialize(new
            {
                ReviewItems = weakContent.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type }), 
                NewItems = generalContent.Select(x => new { x.TargetId, x.DescriptionForAi, x.Type })  
            });

            var strategy = "";
            if (examScore < 50)
                strategy = "Học viên thi TRƯỢT tuần trước (<50 điểm). Yêu cầu: 3 ngày đầu tuần chỉ dùng để ÔN TẬP LẠI (dùng ReviewItems). 3 ngày sau học kiến thức mới nhưng giảm tải. Tăng cường bài tập.";
            else if (examScore < 70)
                strategy = "Học viên đạt trung bình (50-70 điểm). Yêu cầu: Dành 1-2 ngày đầu ôn tập điểm yếu (ReviewItems), sau đó học tiếp lộ trình chuẩn.";
            else
                strategy = "Học viên xuất sắc (>80 điểm). Yêu cầu: Tăng tốc độ, tập trung vào kiến thức mới và khó hơn (NewItems).";

            var promptText = $@"
                Bạn là chuyên gia lập lộ trình TOPIK. Học viên chuẩn bị bước vào TUẦN THỨ {nextWeekIndex}.
                Kết quả tuần trước: {examScore}/100.
                
                CHIẾN LƯỢC ĐIỀU CHỈNH: {strategy}

                *** THÔNG TIN CỐ ĐỊNH ***
                - Mục tiêu: {target}
                - Ngày thứ 7 bắt buộc là: 'WeeklyExam' (Tiêu đề: Kiểm tra định kỳ tuần {nextWeekIndex}).
                - Các ngày thường: Kết hợp 'LearnTheory' và 'VirtualQuiz'.

                *** MENU DỮ LIỆU (CHỈ CHỌN TRONG NÀY) ***
                {menuJson}

                *** OUTPUT JSON FORMAT ***
                (Trả về đúng cấu trúc JSON như các lần trước, chỉ chứa dữ liệu cho 1 tuần duy nhất là WeekIndex {nextWeekIndex})
                {{
                  ""Assessment"": ""Nhận xét ngắn gọn dựa trên điểm số cũ và lời khuyên cho tuần mới"",
                  ""Weeks"": [ {{ ... }} ] 
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