// Infrastructure/Services/Gemini/Question51GeminiPipeline.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.WritingAi
{
    public sealed class Question51GeminiPipeline : IQuestion51Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public Question51GeminiPipeline(
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
            Question51RequestDto request,
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

            // ── 3. Lấy đề từ QuestionBank qua QuestionId ──────────────────
            var question = await _questionBankRepo.GetByIdAsync(
                writingAnswer.QuestionId, ct);

            if (question is null)
                throw new InvalidOperationException("Không tìm thấy câu hỏi.");

            var explanation = question.Explanation?.Trim() ?? "";

            // ── 4. Parse AnswerContent thành 2 câu trả lời ────────────────
            // Format: "㉠: câu 1\n㉡: câu 2" hoặc "㉠: câu 1 ㉡: câu 2"
            string content = writingAnswer.AnswerContent ?? "";
            string answer1 = "", answer2 = "";

            // Tách chuỗi dựa trên các ký hiệu marker, giữ lại marker trong mảng kết quả
            // Bao gồm: ㉠, ㉡ (Chuẩn), ᄀ, ᄂ (Choseong), ㄱ, ㄴ (Jamo)
            string[] parts = Regex.Split(content, @"([㉠㉡ᄀᄂㄱㄴ])");
            string currentMarker = "";

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part == "㉠" || part == "ᄀ" || part == "ㄱ")
                {
                    currentMarker = "㉠";
                }
                else if (part == "㉡" || part == "ᄂ" || part == "ㄴ")
                {
                    currentMarker = "㉡";
                }
                else
                {
                    // Phần này là nội dung sau marker
                    // Loại bỏ dấu hai chấm (cả chuẩn : và full-width ：) và khoảng trắng/xuống dòng
                    string cleanedText = part.Trim().TrimStart(':', '：').Trim();
                    
                    if (currentMarker == "㉠") answer1 = cleanedText;
                    else if (currentMarker == "㉡") answer2 = cleanedText;
                }
            }

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
QUESTION_EXPLANATION (nội dung câu hỏi 51):
{explanation}

USER_ANSWER_㉠:
{answer1}

USER_ANSWER_㉡:
{answer2}
""";

            string? dbConfigJson = await _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_51_PROMPT");
            var promptConfig = new Writing51AiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    var parsedConfig = JsonSerializer.Deserialize<Writing51AiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception ex)
                {
                    _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_51_PROMPT").ContinueWith(t => { /* Fallback to old key if needed */ });
                }
            }

            string finalInstruction = BuildSystemInstruction(promptConfig);
            var systemPrompt = finalInstruction + "\n\n" + BuildOutputSchema();

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                systemPrompt,
                maxOutputTokens: 3000,
                temperature: 0.2,
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

        // ── GUARD: Kiểm tra bài trống ──────────────────────────────────────
        /// <summary>
        /// Trả về true nếu bài nộp trống hoặc quá ngắn:
        /// - WordCount == 0, hoặc
        /// - Cả 2 ô đều rỗng/khoảng trắng, hoặc
        /// - Tổng ký tự của 2 ô <= 5 (coi như chưa làm)
        /// </summary>
        private static bool IsEmptySubmission(int wordCount)
    => wordCount <= 5;
        /// <summary>
        /// Tạo JSON response cho bài nộp trống, khớp schema prompt câu 51.
        /// Score = 0, evaluation = "incorrect", không có suggestions.
        /// </summary>
        private static string BuildEmptySubmissionJson() =>
            """
            {
              "totalScore": 0,
              "results": [
                {
                  "blank_id": "㉠",
                  "user_answer": "",
                  "score": 0,
                  "evaluation": "incorrect",
                  "feedback": "Bạn chưa điền câu trả lời cho ô này. Vui lòng nhập nội dung trước khi nộp bài.",
                  "suggestions": []
                },
                {
                  "blank_id": "㉡",
                  "user_answer": "",
                  "score": 0,
                  "evaluation": "incorrect",
                  "feedback": "Bạn chưa điền câu trả lời cho ô này. Vui lòng nhập nội dung trước khi nộp bài.",
                  "suggestions": []
                }
              ]
            }
            """;

        // ── SCORE HELPERS ──────────────────────────────────────────────────

        /// <summary>
        /// AI chấm thang 0-10 (totalScore).
        /// Convert sang %: totalScore / 10 * 100.
        /// Fallback: cộng score từng blank (max 10) → / 10 * 100.
        /// </summary>
        private static double CalculatePercentageScore(JsonElement json)
        {
            // Ưu tiên totalScore
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetDouble(out var t))
                return Math.Clamp(t / 10.0 * 100.0, 0, 100);

            // Fallback: cộng score từng blank trong results
            double sum = 0;
            if (json.TryGetProperty("results", out var results))
                foreach (var item in results.EnumerateArray())
                    if (item.TryGetProperty("score", out var s) && s.TryGetDouble(out var v))
                        sum += Math.Clamp(v, 0, 5); // mỗi blank tối đa 5đ

            // Tổng tối đa = 10 → / 10 * 100 = 100%
            return Math.Clamp(sum / 10.0 * 100.0, 0, 100);
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

        // ── PROMPT ─────────────────────────────────────────────────────────
        private static string BuildSystemInstruction(Writing51AiPromptConfigDto config) =>
            $@"{config.Persona}

=== VỀ CÂU 51 ===
{config.QuestionOverview}

=== BAREM CHẤM ĐIỂM CHI TIẾT (Tối đa 5 điểm/chỗ trống) ===
Dựa trên tiêu chuẩn chấm thi, hãy đánh giá bài làm của học sinh qua 3 tiêu chí sau:

1. Nội dung & Ngữ cảnh ({config.ContentContext.MaxScore}đ):
{config.ContentContext.Description}

2. Từ vựng & Ngữ pháp ({config.VocabGrammar.MaxScore}đ):
{config.VocabGrammar.Description}

3. Hình thức & Quy tắc ({config.FormRules.MaxScore}đ):
{config.FormRules.Description}

=== QUY TẮC ĐUÔI CÂU VÀ NGÔN NGỮ ===
{config.FormalEndingRules}

=== QUY TẮC DẤU CÂU (PUNCTUATION RULE) ===
{config.PunctuationRules}

=== CÁCH VIẾT FEEDBACK ===
{config.FeedbackRequirements}

QUAN TRỌNG: 
- Nếu phát hiện dùng sai đuôi câu (như -아요/어요), hãy đánh giá là 'incorrect' hoặc 'partial' và trừ điểm nặng ở phần Hình thức.
- Nếu ghi thêm dấu câu ở cuối (như '.', '?'), trừ 1 điểm ở phần Hình thức.
- Bài làm của học sinh cho mỗi ô trống CHỈ ĐƯỢC PHÉP là 1 câu duy nhất.";

        private static string BuildOutputSchema() =>
            @"=== OUTPUT JSON SCHEMA BẮT BUỘC ===
Bạn BẮT BUỘC phải trả về đúng cấu trúc JSON dưới đây. TUYỆT ĐỐI KHÔNG ĐƯỢC THAY ĐỔI CẤU TRÚC, KHÔNG THÊM BỚT TRƯỜNG, KHÔNG THAY ĐỔI TÊN TRƯỜNG. CHỈ TRẢ VỀ JSON.

{
  ""totalScore"": <tổng 2 blank, 0-10>,
  ""results"": [
    {
      ""blank_id"": ""㉠"",
      ""user_answer"": ""<câu người dùng>"",
      ""score"": <0-5>,
      ""evaluation"": ""correct|incorrect|partial"",
      ""feedback"": ""<tiếng Việt, tối đa 4 câu>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do tiếng Việt ngắn>""]
    },
    {
      ""blank_id"": ""㉡"",
      ""user_answer"": ""<câu người dùng>"",
      ""score"": <0-5>,
      ""evaluation"": ""correct|incorrect|partial"",
      ""feedback"": ""<tiếng Việt, tối đa 4 câu>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do tiếng Việt ngắn>""]
    }
  ]
}

CRITICAL:
- ""suggestions"" chỉ khi evaluation != ""correct""
- Trả về ONLY JSON, KHÔNG markdown
- JSON phải HOÀN CHỈNH
- ""feedback"" tối đa 150 ký tự";
    }
}