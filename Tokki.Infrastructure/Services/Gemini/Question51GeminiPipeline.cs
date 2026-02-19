// Infrastructure/Services/Gemini/Question51GeminiPipeline.cs
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.Gemini
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

            // ── 2. Lấy đề từ QuestionBank qua QuestionId ──────────────────
            var question = await _questionBankRepo.GetByIdAsync(
                writingAnswer.QuestionId, ct);

            if (question is null)
                throw new InvalidOperationException("Không tìm thấy câu hỏi.");

            var explanation = question.Explanation?.Trim() ?? "";

            // ── 3. Parse AnswerContent thành 2 câu trả lời ────────────────
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

            // ── 4. Gọi Gemini ──────────────────────────────────────────────
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
                maxOutputTokens: 4096,
                temperature: 0.3,
                ct);

            var feedbackJson = GeminiRestClient.ParseJsonRobust(raw);
            int score = CalculateScore(feedbackJson);

            // ── 5. Update Score + AiAnalysisJson + GradedAt ───────────────
            writingAnswer.Score = score;
            writingAnswer.AiAnalysisJson = raw;
            writingAnswer.GradedAt = DateTime.UtcNow;

            _writingRepo.UpdateAsync(writingAnswer);
            await _writingRepo.SaveChangesAsync(ct);

            return (feedbackJson, score);
        }

        // ── Tính điểm: câu 51 tổng 10 điểm (mỗi blank 5đ) ─────────────
        private static int CalculateScore(JsonElement json)
        {
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetInt32(out var t))
                return Math.Clamp(t, 0, 10);

            int sum = 0;
            if (json.TryGetProperty("results", out var results))
                foreach (var item in results.EnumerateArray())
                    if (item.TryGetProperty("score", out var s) && s.TryGetInt32(out var v))
                        sum += Math.Clamp(v, 0, 5); // mỗi blank tối đa 5đ

            return Math.Clamp(sum, 0, 10);
        }

        // ── PROMPT ─────────────────────────────────────────────────────
        private static string BuildSystemInstruction() => """
Bạn là giáo viên chấm thi TOPIK II Writing câu 51, giảng dạy cho học sinh Việt Nam.

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

Nếu phát hiện BẤT KỲ đuôi SAI nào → evaluation = "incorrect", trừ 2đ văn phong
=== CÁCH VIẾT FEEDBACK ===
- "feedback" PHẢI bằng TIẾNG VIỆT, tối đa 4 câu ngắn gọn
- Giải thích ngữ cảnh: tại sao câu đó đúng/sai
- Ví dụ: "Vì đây là thông báo tìm kiếm thành viên mới trong câu lạc bộ nên cụm từ '신입 회원을 모집합니다' hoàn toàn phù hợp với ngữ cảnh."

=== OUTPUT JSON ===
{
  "totalScore": <tổng 2 blank, 0-10>,
  "results": [
    {
      "blank_id": "㉠",
      "user_answer": "<câu người dùng>",
      "score": <0-5>,
      "evaluation": "correct|incorrect|partial",
      "feedback": "<tiếng Việt, tối đa 4 câu>",
      "suggestions": ["<tiếng Hàn> — <lý do tiếng Việt ngắn>"]
    },
    {
      "blank_id": "㉡",
      "user_answer": "<câu người dùng>",
      "score": <0-5>,
      "evaluation": "correct|incorrect|partial",
      "feedback": "<tiếng Việt, tối đa 4 câu>",
      "suggestions": ["<tiếng Hàn> — <lý do tiếng Việt ngắn>"]
    }
  ]
}

CRITICAL:
- "suggestions" chỉ khi evaluation != "correct"
- Trả về ONLY JSON, KHÔNG markdown
- JSON phải HOÀN CHỈNH
- "feedback" tối đa 150 ký tự
""";
    }
}