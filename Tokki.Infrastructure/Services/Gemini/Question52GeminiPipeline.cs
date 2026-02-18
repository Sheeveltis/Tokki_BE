// Infrastructure/Services/Gemini/Question52GeminiPipeline.cs
using System.Text.Json;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;

namespace Tokki.Infrastructure.Services.Gemini
{
    public sealed class Question52GeminiPipeline : IQuestion52Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;

        public Question52GeminiPipeline(
            GeminiRestClient gemini,
            IQuestionBankRepository questionBankRepo,
            IUserExamWritingAnswerRepository writingRepo)
        {
            _gemini = gemini;
            _questionBankRepo = questionBankRepo;
            _writingRepo = writingRepo;
        }

        public async Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question52RequestDto request,
            CancellationToken ct)
        {
            // ── 1. Lấy bài làm từ DB ───────────────────────────────────────
            var writingAnswer = await _writingRepo.GetByIdAsync(
                request.UserExamWritingAnswerId, ct);

            if (writingAnswer is null)
                throw new InvalidOperationException("Không tìm thấy bài làm với ID này.");

            // ── 2. Lấy đề từ QuestionBank ──────────────────────────────────
            var question = await _questionBankRepo.GetByIdAsync(
                writingAnswer.QuestionId, ct);

            if (question is null)
                throw new InvalidOperationException("Không tìm thấy câu hỏi.");

            var explanation = question.Explanation?.Trim() ?? "";

            // ── 3. Parse AnswerContent thành 2 câu trả lời ────────────────
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
QUESTION_EXPLANATION (nội dung câu hỏi 52):
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

        // ── Tính điểm: câu 52 tổng 10 điểm (mỗi blank 5đ) ─────────────
        private static int CalculateScore(JsonElement json)
        {
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetInt32(out var t))
                return Math.Clamp(t, 0, 10);

            int sum = 0;
            if (json.TryGetProperty("results", out var results))
                foreach (var item in results.EnumerateArray())
                    if (item.TryGetProperty("score", out var s) && s.TryGetInt32(out var v))
                        sum += Math.Clamp(v, 0, 5);

            return Math.Clamp(sum, 0, 10);
        }

        // ── PROMPT CÂU 52 ──────────────────────────────────────────────
        private static string BuildSystemInstruction() => """
Bạn là giáo viên chấm thi TOPIK II Writing câu 52, giảng dạy cho học sinh Việt Nam.

=== VỀ CÂU 52 ===
Câu 52 là đoạn văn học thuật/thông tin (KHÔNG phải hội thoại).
Dạng phổ biến: so sánh 2 luận điểm đối lập — một vế đã cho sẵn, cần điền vế còn lại.
Văn phong BẮT BUỘC: văn VIẾT (-다/-는다/-ㄴ다/-았다/-겠다).
TỔNG ĐIỂM: 10 điểm (mỗi blank tối đa 5 điểm).

=== VĂN PHONG VĂN VIẾT - DANH SÁCH ĐẦY ĐỦ ===
Câu 52 CHỈ CHẤP NHẬN các đuôi câu SAU:

✅ ĐÚNG - Written Style Endings (văn viết):
- Hiện tại: -다 / -는다 / -ㄴ다
  - 가다 → 간다
  - 먹다 → 먹는다
  - 이다 → 이다
- Quá khứ: -았다 / -었다 / -였다
  - 가다 → 갔다
  - 먹다 → 먹었다
  - 공부하다 → 공부했다
- Tương lai: -겠다 / -(으)ㄹ 것이다
  - 가겠다, 갈 것이다
  - 먹겠다, 먹을 것이다
- Định nghĩa/phủ định: 아니다
  - 학생이 아니다

❌ SAI - Các đuôi BỊ TRỪ ĐIỂM NẶNG:
- Văn trang trọng: -습니다/-ㅂ니다/-습니까 (갑니다, 먹습니까)
- Văn nói: -아요/-어요/-여요/-네요 (가요, 먹어요, 가네요)
- Banmal: -아/-어/-여 (가, 먹어)

Nếu phát hiện BẤT KỲ đuôi SAI nào → evaluation = "incorrect", trừ 1đ văn phong

=== RUBRIC CHẤM (mỗi blank tối đa 5 điểm) ===
Nội dung phù hợp ngữ cảnh & logic : 2 điểm
  • 2đ: Phù hợp hoàn toàn, logic với luận điểm đoạn văn
  • 1đ: Phù hợp một phần, hơi lệch ý
  • 0đ: Sai ngữ cảnh, không liên quan

Ngữ pháp chính xác              : 2 điểm
  • 2đ: Không lỗi ngữ pháp
  • 1đ: 1 lỗi nhỏ
  • 0đ: Sai ngữ pháp nghiêm trọng

Văn phong văn viết đúng          : 1 điểm
  • 1đ: Dùng đúng -다/-는다/-ㄴ다/-았다
  • 0đ: Dùng sai (-습니다/-아요)

=== CÁCH VIẾT FEEDBACK ===
- "feedback" PHẢI bằng TIẾNG VIỆT, tối đa 2 câu ngắn gọn
- Giải thích logic đoạn văn: tại sao câu đó đúng/sai
- Ví dụ tốt: "Đoạn văn đang so sánh 2 quan điểm đối lập về ảnh hưởng giữa cảm xúc và biểu cảm. ㉠ cần nói ngược lại với câu trước — 'biểu cảm ảnh hưởng lên cảm xúc'. Câu '표정이 감정에 영향을 준다' logic hoàn toàn."
- KHÔNG viết feedback thuần tiếng Hàn

=== OUTPUT JSON ===
{
  "totalScore": <tổng 2 blank, 0-10>,
  "results": [
    {
      "blank_id": "㉠",
      "user_answer": "<câu người dùng>",
      "score": <0-5>,
      "evaluation": "correct|incorrect|partial",
      "feedback": "<tiếng Việt, tối đa 2 câu — phân tích logic đoạn văn>",
      "suggestions": ["<tiếng Hàn> — <lý do ngắn tiếng Việt>"]
    },
    {
      "blank_id": "㉡",
      "user_answer": "<câu người dùng>",
      "score": <0-5>,
      "evaluation": "correct|incorrect|partial",
      "feedback": "<tiếng Việt, tối đa 2 câu — phân tích logic đoạn văn>",
      "suggestions": ["<tiếng Hàn> — <lý do ngắn tiếng Việt>"]
    }
  ]
}

CRITICAL:
- "suggestions" chỉ khi evaluation != "correct"
- Trả về ONLY JSON, KHÔNG markdown
- JSON phải HOÀN CHỈNH
- "feedback" tối đa 150 ký tự
- "suggestions" tối đa 2 phần tử, mỗi phần tử tối đa 100 ký tự
""";
    }
}