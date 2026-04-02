// Infrastructure/Services/Gemini/Question51GeminiPipeline.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
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

        public Question51GeminiPipeline(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiOptions> options,
            IQuestionBankRepository questionBankRepo,
            IUserExamWritingAnswerRepository writingRepo)
        {
            _gemini = new GeminiRestClient(httpClientFactory, options.Value.Writing);
            _questionBankRepo = questionBankRepo;
            _writingRepo = writingRepo;
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
            // Format: "㉠: câu 1\n㉡: câu 2"
            var lines = writingAnswer.AnswerContent
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string answer1 = "", answer2 = "";
            foreach (var line in lines)
            {
                if (line.StartsWith("㉠:") || line.StartsWith("ᄀ:"))
                    answer1 = line.Substring(line.IndexOf(':') + 1).Trim();
                else if (line.StartsWith("㉡:") || line.StartsWith("ᄂ:"))
                    answer2 = line.Substring(line.IndexOf(':') + 1).Trim();
            }

            // ── 4b. Kiểm tra bài nộp trống — KHÔNG gọi Gemini ─────────────
            if (IsEmptySubmission(writingAnswer.WordCount))
            {
                var emptyRaw = BuildEmptySubmissionJson();
                var emptyFeedback = GeminiRestClient.ParseJsonRobust(emptyRaw);

                writingAnswer.Score = 0;
                writingAnswer.AiAnalysisJson = emptyRaw;
                writingAnswer.GradedAt = DateTime.UtcNow;

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

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                BuildSystemInstruction(),
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
            writingAnswer.GradedAt = DateTime.UtcNow;

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
        private static string BuildSystemInstruction() =>
            @"Bạn là giáo viên chấm thi TOPIK II Writing câu 51, giảng dạy cho học sinh Việt Nam.

=== VỀ CÂU 51 ===
Câu 51 điền vào 2 chỗ trống (㉠ và ㉡) trong hội thoại/thông báo trang trọng.
Văn phong BẮT BUỘC: thể TRANG TRỌNG (-습니다/-ㅂ니다/-(으)세요).
TỔNG ĐIỂM: 10 điểm (mỗi blank tối đa 5 điểm).

=== RUBRIC CHẤM (mỗi blank tối đa 5 điểm) ===
Nội dung phù hợp ngữ cảnh : 2 điểm
Ngữ pháp chính xác        : 1 điểm
Văn phong trang trọng     : 2 điểm

=== VĂN PHONG TRANG TRỌNG - DANH SÁCH ĐẦY ĐỦ ===
Câu 51 CHỈ CHẤP NHẬN các đuôi câu SAU:

✅ ĐÚNG - Formal Polite Endings:
- Tuyên bố: -습니다 / -ㅂ니다
- Nghi vấn: -습니까 / -ㅂ니까
- Yêu cầu: -(으)십시오
- Đề nghị: -(으)ㅂ시다 / -읍시다
- Tôn kính: -(으)세요 / -(으)십니다 / -(으)십니까
- Copula: 입니다 / 입니까 / 이십니까

❌ SAI - Các đuôi BỊ TRỪ ĐIỂM NẶNG:
- Văn nói: -아요/-어요/-여요 (가요, 먹어요)
- Văn viết: -다/-는다/-ㄴ다 (간다, 먹는다)
- Banmal: -아/-어/-여 (가, 먹어)

Nếu phát hiện BẤT KỲ đuôi SAI nào → evaluation = ""incorrect"", trừ 2đ văn phong

=== CÁCH VIẾT FEEDBACK ===
- ""feedback"" PHẢI bằng TIẾNG VIỆT, tối đa 4 câu ngắn gọn
- Giải thích ngữ cảnh: tại sao câu đó đúng/sai
- Ví dụ: ""Vì đây là thông báo tìm kiếm thành viên mới trong câu lạc bộ nên cụm từ '신입 회원을 모집합니다' hoàn toàn phù hợp với ngữ cảnh.""

=== OUTPUT JSON ===
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