using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services
{
    public class AiRoadmapVer2Service : IAiRoadmapVer2Service
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _geminiOptions;
        private readonly ILogger<AiRoadmapVer2Service> _logger;

        public AiRoadmapVer2Service(
            HttpClient httpClient, 
            IOptions<GeminiOptions> geminiOptions,
            ILogger<AiRoadmapVer2Service> logger)
        {
            _httpClient = httpClient;
            _geminiOptions = geminiOptions.Value;
            _logger = logger;
        }

        public async Task<List<string>> SequenceWeaknessesAsync(
            List<string> questionTypeIds, 
            CurrentTopikLevel currentLevel, 
            TargetAimLevel targetLevel, 
            List<QuestionTypeMenuDto> typeMenu, 
            CancellationToken token = default)
        {
            _logger.LogInformation("Sắp xếp trình tự cho {Count} điểm yếu.", questionTypeIds.Count);
            
            var menuDetails = string.Join("\n", typeMenu.Select(m => $"- ID: {m.QuestionTypeId}, Mã: {m.Code}, Tên: {m.Name}, Kỹ năng: {m.Skill}"));

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
            TargetAimLevel targetAim, 
            CurrentTopikLevel currentLevel, 
            int weekIndex, 
            int totalWeeks,
            List<string> focusTypeIds, 
            List<string> deferredTypeIds, 
            List<QuestionTypeMenuDto> weakTypeInfos, 
            List<QuestionTypeMenuDto> fullMenu, 
            CancellationToken token = default)
        {
            var focusDetails = string.Join("\n", weakTypeInfos.Select(f => $"- {f.Code}: {f.Name} ({f.Skill}) [ID: {f.QuestionTypeId}]"));
            var deferredDetails = deferredTypeIds.Any() 
                ? "DANH SÁCH TẠM HOÃN (Đã sai 2 tuần liên tiếp, cần tạm dừng để học dạng mới): " + string.Join(", ", fullMenu.Where(m => deferredTypeIds.Contains(m.QuestionTypeId)).Select(m => m.Name))
                : "Không có dạng bài tồn đọng đặc biệt.";
            
            string prompt = $@"
            Bạn là chuyên gia thiết kế lộ trình học TOPIK cấp độ {targetAim}. 
            Hãy thiết kế nội dung học tập TUYỆT VỜI cho TUẦN {weekIndex}.

            THÔNG TIN HỌC VIÊN:
            - Mục tiêu: {targetAim}
            - Trình độ hiện tại: {currentLevel}
            - 5 Dạng bài trọng tâm của tuần này: 
            {focusDetails}
            - {deferredDetails}

            QUY TẮC THIẾT KẾ & PHÂN BỔ (QUAN TRỌNG):
            1. PHÂN BỔ DẠNG BÀI (TUÂN THỦ FIFO): 5 dạng bài trọng tâm trên được đánh số từ 1 đến 5 theo thứ tự ưu tiên. Bạn PHẢI phân bổ như sau:
               - THỨ 2 (Day 1): Dạng bài 1 (Dạng đầu tiên trong danh sách).
               - THỨ 3 (Day 2): Dạng bài 2.
               - THỨ 4 (Day 3): Dạng bài 3.
               - THỨ 5 (Day 4): Dạng bài 4.
               - THỨ 6 (Day 5): Dạng bài 5.
            2. CHI TIẾT CÁC NGÀY:
               - THỨ 2 (DayIndex 1): 
                 * NẾU weekIndex > 1: BẮT BUỘC có 1 task ""Document"" đầu tiên (questionTypeId: null). Tiêu đề: ""Tái khởi động và Phân tích nội dung tuần {weekIndex}"". Nội dung (HTML): Tóm tắt tiến độ tuần trước dựa trên kết quả bài thi tuần. ĐẶC BIỆT, nếu có danh sách '{deferredDetails}', bạn phải giải thích khéo léo cho học viên rằng các dạng này sẽ tạm thời được hoãn lại để họ tập trung vào các dạng bài mới, giúp đổi mới tư duy tránh bị quá tải trước khi quay lại ôn tập sau. Cuối cùng, nêu bật mục tiêu học tập 5 dạng bài của tuần này.
                 * Sau đó là 2 task cho Dạng bài 1: LearnTheory và VirtualQuiz.
               - THỨ 3 - THỨ 6 (DayIndex 2-5): Mỗi ngày học 1 dạng bài duy nhất theo thứ tự trên. Mỗi ngày gồm 2 task: LearnTheory và VirtualQuiz.
               - THỨ 7 (TỔNG ÔN TẬP - DayIndex 6): 
                 * 1 Task LearnTheory (questionTypeId: null): Tiêu đề BẮT BUỘC là ""Tóm tắt tinh hoa và chiến thuật các dạng bài tuần {weekIndex}"".
                 * Các Task VirtualQuiz: Mỗi task tương ứng với 1 QuestionTypeId khác nhau từ danh sách 5 dạng của tuần này.
               - CHỦ NHẬT (THI THỬ - DayIndex 7): 
                 * 1 Task WeeklyExam (questionTypeId: null): Tiêu đề ""Bài kiểm tra tổng hợp tuần {weekIndex}"".
            3. QUY ĐỊNH VỀ ID: 
               - CHỈ sử dụng QuestionTypeId từ danh sách 5 dạng bài đã cho ở trên. 
               - Các task ""Document"" và ""WeeklyExam"" và task LearnTheory của THỨ 7 BẮT BUỘC phải để ""questionTypeId"": null.
               - KHÔNG tự sinh ID mới, KHÔNG dùng ID không có trong danh sách.

            YÊU CẦU NỘI DUNG CHO TASK LearnTheory (CỰC KỲ QUAN TRỌNG):
            Phần ""content"" của task LearnTheory phải dài, chi tiết và được trình bày bằng HTML đẹp mắt, chia rõ làm 2 phần lớn:
            1. GIOI THIEU (Giới thiệu):
               - Mô tả chi tiết dạng bài này gồm những cái gì, cấu trúc thường gặp.
               - Những gì học viên cần tập trung nhất khi làm dạng này (Key focus).
               - Phương pháp học và phương pháp làm bài hiệu quả (Tips & Strategies).
            2. LY THUYET (Lý thuyết):
               - Trình bày kiến thức chuyên sâu.
               - Những điểm lưu ý quan trọng, các lỗi sai thường gặp.
               - Các ngữ pháp thông dụng thường xuyên xuất hiện trong dạng bài này.
               - Các kiến thức bổ trợ hoặc phát triển thêm để nâng cao trình độ.

            CỐ ĐỊNH CÁC LOẠI TASK: LearnTheory, VirtualQuiz, WeeklyExam, Document.

            CẤU TRÚC JSON MẪU:
            {{
              ""assessment"": ""Nhận xét chuyên sâu tuần {weekIndex}..."",
              ""weeks"": [
                {{
                  ""weekIndex"": {weekIndex},
                  ""weekGoal"": ""Mục tiêu đạt được sau tuần này"",
                  ""days"": [
                    {{
                      ""dayIndex"": 1,
                      ""tasks"": [
                        {{
                          ""title"": ""Tái khởi động và Phân tích nội dung tuần {weekIndex}"",
                          ""taskType"": ""Document"",
                          ""questionTypeId"": null,
                          ""content"": ""HTML tóm tắt...""
                        }},
                        ...
                      ]
                    }},
                    {{
                      ""dayIndex"": 6,
                      ...
                    }}
                  ]
                }}
              ]
            }}";

            return await CallGeminiApiAsync(prompt);
        }

        private async Task<AiRoadmapResponse?> CallGeminiApiAsync(string promptText)
        {
            var config = _geminiOptions.Roadmap;
            string url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.Model}:generateContent?key={config.ApiKey}";

            var requestBody = new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = promptText } } } },
                generationConfig = new { response_mime_type = "application/json" }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var rawJson = result.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                rawJson = rawJson?.Replace("```json", "").Replace("```", "").Trim();

                return JsonSerializer.Deserialize<AiRoadmapResponse>(rawJson!, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi AI API.");
                return null;
            }
        }

        private async Task<string?> CallGeminiTextAsync(string promptText)
        {
            var config = _geminiOptions.Roadmap;
            string url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.Model}:generateContent?key={config.ApiKey}";

            var requestBody = new { contents = new[] { new { role = "user", parts = new[] { new { text = promptText } } } } };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi AI Text API.");
                return null;
            }
        }
    }
}
