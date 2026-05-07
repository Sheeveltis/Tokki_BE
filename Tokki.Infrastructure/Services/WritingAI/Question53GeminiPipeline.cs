// Infrastructure/Services/Gemini/Question53GeminiPipeline.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.WritingAi
{
    public sealed class Question53GeminiPipeline : IQuestion53Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public Question53GeminiPipeline(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiOptions> options,
            IQuestionBankRepository questionBankRepo,
            IUserExamWritingAnswerRepository writingRepo,
            ISystemConfigRepository systemConfigRepo)
        {
            _gemini = new GeminiRestClient(httpClientFactory, options.Value.Writing);
            _questionBankRepo = questionBankRepo;
            _writingRepo = writingRepo;
            _systemConfigRepo = systemConfigRepo;
        }

        public async Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question53RequestDto request,
            CancellationToken ct)
        {
            // ── 1. Lấy bài làm từ DB ───────────────────────────────────────
            var writingAnswer = await _writingRepo.GetByIdAsync(
                request.UserExamWritingAnswerId, ct);

            if (writingAnswer is null)
                throw new InvalidOperationException("Không tìm thấy bài làm với ID này.");

            // ── 2. Lấy maxMark ─────────────────────────────────────────────
            double maxMark = await _writingRepo.GetMaxMarkByOrderIndexAsync(
                writingAnswer.UserExamId, writingAnswer.OrderIndex, ct);

            // ── 3. Lấy đề từ QuestionBank ──────────────────────────────────
            var question = await _questionBankRepo.GetByIdAsync(
                writingAnswer.QuestionId, ct);

            if (question is null)
                throw new InvalidOperationException("Không tìm thấy câu hỏi.");

            var explanation = question.Explanation?.Trim() ?? "";

            // ── 4. Đếm số ký tự bài viết ───────────────────────────────────
            int charCount = CountChars(writingAnswer.AnswerContent);

            // ── 4b. Kiểm tra bài nộp trống — KHÔNG gọi Gemini ─────────────
            if (IsEmptySubmission(writingAnswer.WordCount))
            {
                var emptyRaw = BuildEmptySubmissionJson();
                var emptyFeedback = GeminiRestClient.ParseJsonRobust(emptyRaw);

                writingAnswer.Score = 0;
                writingAnswer.AiAnalysisJson = emptyRaw;
                writingAnswer.GradedAt = DateTime.UtcNow.AddHours(7);

                _writingRepo.UpdateAsync(writingAnswer);
                await _writingRepo.SaveChangesAsync(ct);

                return (emptyFeedback, 0);
            }

            // ── 5. Gọi Gemini ──────────────────────────────────────────────
            var userText = $"""
QUESTION_EXPLANATION (nội dung câu hỏi 53 - mô tả biểu đồ/khảo sát):
{explanation}

USER_ESSAY (bài viết của thí sinh - {charCount} ký tự, đã đếm kể cả khoảng trắng):
{writingAnswer.AnswerContent}

POLISHED_VERSION_REQUIREMENT:
- polishedVersion BẮT BUỘC dài 200-300 ký tự (đếm kể cả khoảng trắng)
- polishedVersion TUYỆT ĐỐI KHÔNG xuống dòng — viết liền một đoạn duy nhất
- Trước khi xuất JSON, tự đếm lại: nếu < 200 ký tự → thêm chi tiết; nếu > 300 ký tự → rút gọn
- CHỈ xuất JSON khi polishedVersion đã đạt đúng 200-300 ký tự
""";

            string? dbConfigJson = await _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_53_PROMPT");
            var promptConfig = new Writing53AiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    var parsedConfig = JsonSerializer.Deserialize<Writing53AiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception) { }
            }

            string finalInstruction = BuildSystemInstruction(promptConfig);
            var systemPrompt = finalInstruction + "\n\n" + BuildOutputSchema();

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                systemPrompt,
                maxOutputTokens: 16384,   // Câu 53 cần nhiều token hơn vì feedback chi tiết
                temperature: 0.2,        // Thấp hơn để chấm điểm nhất quán
                ct);

            var feedbackJson = GeminiRestClient.ParseJsonRobust(raw);
            double percentageScore = CalculatePercentageScore(feedbackJson);

            // Nếu maxMark không hợp lệ: vẫn lưu feedback đầy đủ, chỉ score = -1
            int actualScore = maxMark <= 0
                ? -1
                : CalculateActualScore(percentageScore, maxMark);

            // ── 6. Update Score + AiAnalysisJson + GradedAt ───────────────
            writingAnswer.Score = actualScore;
            writingAnswer.AiAnalysisJson = raw;
            writingAnswer.GradedAt = DateTime.UtcNow.AddHours(7);

            _writingRepo.UpdateAsync(writingAnswer);
            await _writingRepo.SaveChangesAsync(ct);

            return (feedbackJson, actualScore);
        }

        // ── CHAR COUNT ─────────────────────────────────────────────────────
        /// <summary>
        /// Đếm tổng số ký tự bao gồm cả khoảng trắng — khớp với cách Gemini đếm.
        /// </summary>
        private static int CountChars(string text)
            => string.IsNullOrEmpty(text) ? 0 : text.Length;

        // ── GUARD: Kiểm tra bài trống ──────────────────────────────────────
        /// <summary>
        /// Trả về true nếu bài nộp trống:
        /// WordCount == 0, hoặc content rỗng/khoảng trắng, hoặc cả 2 điều kiện.
        /// </summary>
        private static bool IsEmptySubmission(int wordCount)
    => wordCount <= 5;

        /// <summary>
        /// Tạo JSON response cho bài nộp trống, khớp schema prompt câu 53.
        /// Tất cả scores = 0, lengthPenalty = tối đa (bài không có ký tự nào),
        /// missingInfo liệt kê toàn bộ bài trống, polishedVersion rỗng.
        /// </summary>
        private static string BuildEmptySubmissionJson() =>
            """
            {
              "totalScore": 0,
              "charCount": 0,
              "lengthPenalty": 30,
              "contentScore": 0,
              "contentFeedback": "Bài làm trống. Bạn chưa viết bất kỳ nội dung nào cho phần mô tả biểu đồ.",
              "missingInfo": [
                "Toàn bộ nội dung bài viết (mở bài, thân bài, kết luận) còn thiếu"
              ],
              "organizationScore": 0,
              "organizationFeedback": "Bài làm trống, không có cấu trúc mở bài - thân bài - kết luận.",
              "structure": {
                "hasIntro": false,
                "hasBody": false,
                "hasConclusion": false
              },
              "languageScore": 0,
              "languageFeedback": "Bài làm trống, không có nội dung ngôn ngữ để đánh giá.",
              "grammarErrors": [],
              "overallFeedback": "Bạn chưa viết bài. Hãy mô tả đầy đủ dữ liệu biểu đồ trong khoảng 200-300 ký tự với cấu trúc mở bài - thân bài - kết luận rõ ràng.",
              "polishedVersion": ""
            }
            """;

        // ── SCORE HELPERS ──────────────────────────────────────────────────

        /// <summary>
        /// AI chấm thang 0-30 (totalScore).
        /// Convert sang %: totalScore / 30 * 100.
        /// Fallback: cộng 3 sub-scores (max 30) → / 30 * 100.
        /// </summary>
        private static double CalculatePercentageScore(JsonElement json)
        {
            // Ưu tiên totalScore (AI đã tính penalty sẵn)
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetDouble(out var t))
                return Math.Clamp(t / 30.0 * 100.0, 0, 100);

            // Fallback: cộng 3 sub-scores
            double sum = 0;
            if (json.TryGetProperty("contentScore", out var content)
                && content.TryGetDouble(out var c))
                sum += c;

            if (json.TryGetProperty("organizationScore", out var org)
                && org.TryGetDouble(out var o))
                sum += o;

            if (json.TryGetProperty("languageScore", out var lang)
                && lang.TryGetDouble(out var l))
                sum += l;

            // content(0-7) + org(0-7) + lang(0-16) = max 30 → / 30 * 100 = 100%
            return Math.Clamp(sum / 30.0 * 100.0, 0, 100);
        }

        /// <summary>
        /// Tính điểm thực tế từ % và maxMark.
        /// Làm tròn, trả số nguyên — không bao giờ trả số thập phân.
        /// </summary>
        private static int CalculateActualScore(double percentageScore, double maxMark)
        {
            double actual = (percentageScore / 100.0) * maxMark;
            return (int)Math.Round(actual, MidpointRounding.AwayFromZero);
        }

        // ── PROMPT CÂU 53 - DYNAMIC FROM CONFIG ───────────────────────────────
        private static string BuildSystemInstruction(Writing53AiPromptConfigDto config) =>
            $@"{config.Persona}

=== VỀ CÂU 53 ===
{config.QuestionOverview}

=== QUY TẮC XUỐNG DÒNG (CỰC KỲ QUAN TRỌNG) ===
{config.NoNewlineRule}

=== VĂN PHONG VĂN VIẾT ===
{config.WritingStyleRules}

=== BỐ CỤC CHUẨN THEO VIỆN GIÁO DỤC HÀN QUỐC ===
{config.LayoutStructure}

=== RUBRIC CHẤM (30 điểm) ===

1. Nội dung & Hoàn thành nhiệm vụ (Content - {config.TaskCompletion.MaxScore}đ):
{config.TaskCompletion.Description}

2. Cấu trúc & Mạch lạc (Organization - {config.Organization.MaxScore}đ):
{config.Organization.Description}

3. Ngữ pháp & Từ vựng (Language - {config.LanguageUsage.MaxScore}đ):
{config.LanguageUsage.Description}

=== CÁCH VIẾT FEEDBACK ===
{config.FeedbackRequirements}";

        private static string BuildOutputSchema() =>
            @"=== OUTPUT JSON SCHEMA BẮT BUỘC ===
Bạn BẮT BUỘC phải trả về đúng cấu trúc JSON dưới đây. TUYỆT ĐỐI KHÔNG ĐƯỢC THAY ĐỔI CẤU TRÚC, KHÔNG THÊM BỚT TRƯỜNG, KHÔNG THAY ĐỔI TÊN TRƯỜNG. CHỈ TRẢ VỀ JSON.

{
  ""totalScore"": <0-30>,
  ""charCount"": <số ký tự thực tế>,
  ""lengthPenalty"": <điểm bị trừ do độ dài, 0 nếu ok>,
  
  ""contentScore"": <0-7>,
  ""contentFeedback"": ""<tiếng Việt - nêu rõ: dạng biểu đồ, câu mở đúng/sai, thông tin đầy đủ/thiếu>"",
  ""missingInfo"": [
    ""<thông tin 1 bị thiếu>"",
    ""<thông tin 2 bị thiếu>""
  ],
  
  ""organizationScore"": <0-7>,
  ""organizationFeedback"": ""<tiếng Việt - đánh giá cấu trúc, câu ghép, từ nối, cách gộp số liệu, có/không xuống dòng>"",
  ""structure"": {
    ""hasIntro"": true,
    ""hasBody"": true,
    ""hasConclusion"": false
  },
  
  ""languageScore"": <0-16>,
  ""languageFeedback"": ""<tiếng Việt - đánh giá văn phong, ngữ pháp, biểu hiện kết quả điều tra>"",
  ""grammarErrors"": [
    {
      ""error"": ""<câu sai>"",
      ""correction"": ""<câu đúng>"",
      ""reason"": ""<giải thích tiếng Việt>""
    }
  ],
  
  ""overallFeedback"": ""<tiếng Việt - tổng kết ngắn: điểm mạnh, điểm yếu, 1-2 gợi ý cải thiện>"",
  
  ""polishedVersion"": ""<bài viết mẫu hoàn chỉnh, BẮT BUỘC đúng 200-300 ký tự kể cả khoảng trắng - sửa tất cả lỗi, bổ sung thông tin thiếu, KHÔNG có ký tự xuống dòng, viết liền một đoạn duy nhất, tự đếm lại trước khi xuất>""
}

=== QUY TẮC CỨNG ===
1. Trả về ONLY JSON, KHÔNG markdown
2. Tất cả feedback TIẾNG VIỆT
3. ""missingInfo"" = [] nếu đầy đủ
4. ""grammarErrors"" = [] nếu không lỗi
5. ""polishedVersion"" BẮT BUỘC có - viết bài mẫu 4-6 câu ghép chuẩn, đủ 200-300 ký tự
6. Chấm điểm KHẮT KHE với thông tin thiếu và sai dạng biểu đồ
7. ""lengthPenalty"" = 0 nếu 200-300 ký tự
8. ""polishedVersion"" BẮT BUỘC đúng 200-300 ký tự (đếm kể cả khoảng trắng)
   - Tự đếm trước khi xuất JSON
   - Nếu chưa đủ 200 ký tự → mở rộng thân bài, thêm số liệu, thêm từ nối
   - Nếu vượt 300 ký tự → lược bớt chi tiết thừa, giữ đủ intro-body-conclusion
9. ""polishedVersion"" TUYỆT ĐỐI KHÔNG chứa ký tự xuống dòng (\n hoặc \r\n)
   - Phải là một chuỗi liên tục, không ngắt đoạn
   - Dùng câu ghép (-으며, -고, -(으)나, -지만) để nối ý thay cho xuống dòng
10. Nếu bài học sinh có xuống dòng → ghi rõ trong organizationFeedback và trừ 1-2 điểm Organization";
    }
}