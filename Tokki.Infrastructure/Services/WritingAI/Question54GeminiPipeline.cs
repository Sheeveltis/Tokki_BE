// Infrastructure/Services/Gemini/Question54GeminiPipeline.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.WritingAi
{
    public sealed class Question54GeminiPipeline : IQuestion54Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public Question54GeminiPipeline(
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
            Question54RequestDto request,
            CancellationToken ct)
        {
            var writingAnswer = await _writingRepo.GetByIdAsync(
                request.UserExamWritingAnswerId, ct);

            if (writingAnswer is null)
                throw new InvalidOperationException("Không tìm thấy bài làm với ID này.");

            double maxMark = await _writingRepo.GetMaxMarkByOrderIndexAsync(
                writingAnswer.UserExamId, writingAnswer.OrderIndex, ct);

            var question = await _questionBankRepo.GetByIdAsync(
                writingAnswer.QuestionId, ct);

            if (question is null)
                throw new InvalidOperationException("Không tìm thấy câu hỏi.");

            var explanation = question.Explanation?.Trim() ?? "";
            int charCount = CountChars(writingAnswer.AnswerContent);

            // ── GUARD: Kiểm tra bài nộp trống — KHÔNG gọi Gemini ──────────
            if (IsEmptySubmission(writingAnswer.AnswerContent, writingAnswer.WordCount))
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

            var userText = $"""
QUESTION_EXPLANATION (nội dung câu hỏi 54 - essay prompt):
{explanation}

USER_ESSAY (bài luận của thí sinh - {charCount} ký tự, đã đếm kể cả khoảng trắng):
{writingAnswer.AnswerContent}

MAX_MARK: {maxMark} điểm

POLISHED_VERSION_REQUIREMENT:
- polishedVersion BẮT BUỘC dài 600-700 ký tự (đếm kể cả khoảng trắng)
- Trước khi xuất JSON, tự đếm lại: nếu < 600 ký tự → thêm supporting details; nếu > 700 ký tự → rút gọn
- CHỈ xuất JSON khi polishedVersion đã đạt đúng 600-700 ký tự
""";

            string? dbConfigJson = await _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_54_PROMPT");
            var promptConfig = new Writing54AiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    var parsedConfig = JsonSerializer.Deserialize<Writing54AiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception) { }
            }

            string finalInstruction = BuildSystemInstruction(promptConfig);
            var systemPrompt = finalInstruction + "\n\n" + BuildOutputSchema();

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                systemPrompt,
                maxOutputTokens: 32768,
                temperature: 0.2,
                ct);

            var feedbackJson = GeminiRestClient.ParseJsonRobust(raw);
            double percentageScore = CalculatePercentageScore(feedbackJson);

            // Nếu maxMark không hợp lệ: vẫn lưu feedback đầy đủ, chỉ score = -1
            int actualScore = maxMark <= 0
                ? -1
                : CalculateActualScore(percentageScore, maxMark);

            writingAnswer.Score = actualScore;
            writingAnswer.AiAnalysisJson = raw;
            writingAnswer.GradedAt = DateTime.UtcNow.AddHours(7);

            _writingRepo.UpdateAsync(writingAnswer);
            await _writingRepo.SaveChangesAsync(ct);

            return (feedbackJson, actualScore);
        }

        // ── CHAR COUNT ──────────────────────────────────────────────────────
        /// <summary>
        /// Đếm tổng số ký tự bao gồm cả khoảng trắng — khớp với cách Gemini đếm.
        /// </summary>
        private static int CountChars(string text)
            => string.IsNullOrEmpty(text) ? 0 : text.Length;

        private static bool IsEmptySubmission(string content, int wordCount)
            => wordCount == 0 || string.IsNullOrWhiteSpace(content);

        /// <summary>
        /// Tạo JSON response cho bài nộp trống, khớp schema prompt câu 54.
        /// Tất cả scores = 0, 3 tasks chưa hoàn thành, polishedVersion rỗng.
        /// </summary>
        private static string BuildEmptySubmissionJson() =>
            """
            {
              "totalScore": 0,
              "charCount": 0,
              "lengthPenalty": 50,
              "essayType": "unknown",
              "contentScore": 0,
              "contentFeedback": "Bài làm trống. Bạn chưa viết bất kỳ nội dung nào cho bài luận.",
              "taskCompletion": {
                "task1": {
                  "completed": false,
                  "feedback": "Chưa hoàn thành — bài làm trống."
                },
                "task2": {
                  "completed": false,
                  "feedback": "Chưa hoàn thành — bài làm trống."
                },
                "task3": {
                  "completed": false,
                  "feedback": "Chưa hoàn thành — bài làm trống."
                }
              },
              "organizationScore": 0,
              "organizationFeedback": "Bài làm trống, không có cấu trúc mở bài - thân bài - kết luận.",
              "structure": {
                "hasIntro": false,
                "hasBody": false,
                "hasConclusion": false,
                "paragraphCount": 0
              },
              "languageScore": 0,
              "languageFeedback": "Bài làm trống, không có nội dung ngôn ngữ để đánh giá.",
              "grammarErrors": [],
              "styleErrors": [],
              "overallFeedback": "Bạn chưa viết bài. Hãy hoàn thành bài luận trong khoảng 600-700 ký tự với đầy đủ 3 tasks và cấu trúc mở bài - thân bài - kết luận rõ ràng.",
              "polishedVersion": ""
            }
            """;

        // ── SCORE HELPERS ──────────────────────────────────────────────────

        /// <summary>
        /// AI chấm thang 0-50 (totalScore).
        /// Convert sang %: totalScore / 50 * 100.
        /// Fallback: content(0-12) + org(0-12) + lang(0-26) = max 50 → / 50 * 100.
        /// </summary>
        private static double CalculatePercentageScore(JsonElement json)
        {
            // Ưu tiên totalScore (AI đã tính penalty sẵn)
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetDouble(out var t))
                return Math.Clamp(t / 50.0 * 100.0, 0, 100);

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

            return Math.Clamp(sum / 50.0 * 100.0, 0, 100);
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

        // ── PROMPT CÂU 54 - DYNAMIC FROM CONFIG ─────────────────────────────────────────────
        private static string BuildSystemInstruction(Writing54AiPromptConfigDto config) =>
            $@"{config.Persona}

=== VỀ CÂU 54 ===
{config.QuestionOverview}

=== 4 DẠNG ĐỀ CHÍNH VÀ CÁCH NHẬN DIỆN ===
{config.QuestionTypes}

=== VĂN PHONG VĂN VIẾT (문어체) ===
{config.WritingStyleRules}

=== CÁCH CẤU TRÚC VÀ CÁC MẪU CÂU CHUẨN ===
{config.LayoutStructure}

=== RUBRIC CHẤM CHÍNH THỨC (50 điểm) ===

1. Nội dung & Hoàn thành nhiệm vụ (Content - {config.ContentCompletion.MaxScore}đ):
{config.ContentCompletion.Description}

2. Cấu trúc & Mạch lạc (Organization - {config.Organization.MaxScore}đ):
{config.Organization.Description}

3. Ngữ pháp & Từ vựng (Language - {config.LanguageUsage.MaxScore}đ):
{config.LanguageUsage.Description}

=== ĐỘ DÀI — TRỪ ĐIỂM NGHIÊM KHẮC ===
{config.LengthPenaltyRules}

=== CÁC LỖI HAY GẶP — PHẢI KIỂM TRA ===
{config.CommonErrors}

=== CÁCH VIẾT FEEDBACK CHO NGƯỜI DÙNG ===
{config.FeedbackRequirements}";

        private static string BuildOutputSchema() =>
            @"=== OUTPUT JSON SCHEMA BẮT BUỘC ===
Bạn BẮT BUỘC phải trả về đúng cấu trúc JSON dưới đây. TUYỆT ĐỐI KHÔNG ĐƯỢC THAY ĐỔI CẤU TRÚC, KHÔNG THÊM BỚT TRƯỜNG, KHÔNG THAY ĐỔI TÊN TRƯỜNG. CHỈ TRẢ VỀ JSON.

{
  ""totalScore"": <0-50>,
  ""charCount"": <số ký tự thực tế kể cả khoảng trắng>,
  ""lengthPenalty"": <điểm bị trừ do độ dài, 0 nếu 600-700 ký tự>,

  ""essayType"": ""<problem_solving / argumentative / topic_explanation / comparison>"",

  ""contentScore"": <0-12>,
  ""contentFeedback"": ""<tiếng Việt — xưng Bạn — đánh giá từng task 1/2/3: đầy đủ không, logic liên kết thế nào, thesis rõ không, ví dụ thuyết phục không, có copy đề không>"",
  ""taskCompletion"": {
    ""task1"": {
      ""completed"": true,
      ""feedback"": ""<tiếng Việt — task 1 đủ nội dung không, cụ thể gì>""
    },
    ""task2"": {
      ""completed"": false,
      ""feedback"": ""<tiếng Việt — task 2 đủ nội dung không, thiếu gì>""
    },
    ""task3"": {
      ""completed"": true,
      ""feedback"": ""<tiếng Việt — task 3 đủ nội dung không, cụ thể gì>""
    }
  },

  ""organizationScore"": <0-12>,
  ""organizationFeedback"": ""<tiếng Việt — xưng Bạn — đánh giá: 서론/본론/결론 rõ không, 담화 표지 có đa dạng không, mạch văn trơn không, intro/conclusion đủ dung lượng không>"",
  ""structure"": {
    ""hasIntro"": true,
    ""hasBody"": true,
    ""hasConclusion"": false,
    ""paragraphCount"": <số đoạn>
  },

  ""languageScore"": <0-26>,
  ""languageFeedback"": ""<tiếng Việt — xưng Bạn — đánh giá văn phong (-다/-는다 đúng không), lỗi ngữ pháp cụ thể, từ vựng phong phú không, có dùng -니까 sai không, cấu trúc câu đa dạng không>"",
  ""grammarErrors"": [
    {
      ""error"": ""<câu/cụm sai>"",
      ""correction"": ""<câu/cụm đúng>"",
      ""reason"": ""<giải thích tiếng Việt ngắn gọn>""
    }
  ],
  ""styleErrors"": [
    ""<câu cụ thể dùng sai văn phong, ví dụ: '갑니다' → nên dùng '간다'>""
  ],

  ""overallFeedback"": ""<tiếng Việt — xưng Bạn — tổng kết: điểm mạnh cụ thể là gì, điểm yếu quan trọng nhất là gì, 2-3 gợi ý cải thiện cụ thể có thể thực hiện ngay>"",

  ""polishedVersion"": ""<bài viết mẫu hoàn chỉnh, BẮT BUỘC đúng 600-700 ký tự kể cả khoảng trắng — sửa tất cả lỗi, bổ sung tasks thiếu, cấu trúc 서론-본론-결론 chuẩn, có 담화 표지 đa dạng, tự đếm lại trước khi xuất>""
}

=== QUY TẮC CỨNG ===
1. Trả về ONLY JSON, KHÔNG có markdown, KHÔNG có text ngoài JSON
2. Tất cả feedback TIẾNG VIỆT, xưng ""Bạn"" (không xưng ""em"")
3. CHẤM KHẮT KHE nhất với tasks thiếu và logic rời rạc — đây là lỗi nghiêm trọng nhất
4. Logic liên kết giữa các tasks LÀ YẾU TỐ QUYẾT ĐỊNH điểm content
5. Copy đề bài nguyên văn → trừ 3-5 điểm content ngay
6. Dùng -니까 trong essay → ghi vào styleErrors và trừ điểm language
7. ""polishedVersion"" BẮT BUỘC phải có, đúng 600-700 ký tự
8. ""lengthPenalty"" = 0 nếu 600-700 ký tự
9. Văn phong sai = trừ languageScore, KHÔNG trừ contentScore
10. Câu ngắn gọn chính xác > Câu dài phức tạp có lỗi
11. Tự đếm polishedVersion: thiếu 600 → thêm supporting details; vượt 700 → rút gọn ví dụ thừa";
    }
}