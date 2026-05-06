// Infrastructure/Services/Gemini/Question52GeminiPipeline.cs
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.WritingAi
{
    public sealed class Question52GeminiPipeline : IQuestion52Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public Question52GeminiPipeline(
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
            Question52RequestDto request,
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

            // ── GUARD: Kiểm tra bài nộp trống — KHÔNG gọi Gemini ──────────
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

            var userText = $"""
QUESTION_EXPLANATION (nội dung câu hỏi 52):
{explanation}

USER_ANSWER_㉠:
{answer1}

USER_ANSWER_㉡:
{answer2}

MAX_MARK: {maxMark} điểm
""";

            string? dbPrompt = await _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_52_PROMPT");
            string finalInstruction = string.IsNullOrWhiteSpace(dbPrompt) ? BuildSystemInstruction() : dbPrompt;
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
        /// Tạo JSON response cho bài nộp trống, khớp schema prompt câu 52.
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
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (item.TryGetProperty("score", out var s) && s.TryGetDouble(out var v))
                        sum += v;
                }
            }

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

        // ── PROMPT CÂU 52 ──────────────────────────────────────────────────
        private static string BuildSystemInstruction() =>
            @"Bạn là giáo viên chấm thi TOPIK II Writing câu 52, giảng dạy cho học sinh Việt Nam.

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

Nếu phát hiện BẤT KỲ đuôi SAI nào → evaluation = ""incorrect"", trừ 1đ văn phong

=== RUBRIC CHẤM (mỗi blank tối đa 5 điểm) ===
Nội dung phù hợp ngữ cảnh & logic : 2 điểm
  • 2đ: Phù hợp hoàn toàn, logic với luận điểm đoạn văn
  • 1đ: Phù hợp một phần, hơi lệch ý
  • 0đ: Sai ngữ cảnh, không liên quan

Ngữ pháp và từ vựng chính xác     : 1 điểm
  • 1đ: Không lỗi ngữ pháp/từ vựng
  • 0đ: Có lỗi ngữ pháp/từ vựng

Văn phong văn viết đúng           : 1 điểm
  • 1đ: Dùng đúng -다/-는다/-ㄴ다/-았다
  • 0đ: Dùng sai (-습니다/-아요)

Ngắn gọn (chỉ 1 câu duy nhất)     : 1 điểm
  • 1đ: Chỉ viết 1 câu
  • 0đ: Viết nhiều hơn 1 câu

=== LỖI DẤU CÂU (PUNCTUATION RULE) ===
TUYỆT ĐỐI KHÔNG được dùng dấu câu ở cuối câu (như dấu chấm ""."", dấu chấm hỏi ""?"", v.v.).
Nếu người dùng viết CÓ DẤU CÂU ở cuối → BỊ TRỪ 1 ĐIỂM vào tổng điểm của blank đó.

=== CÁCH VIẾT FEEDBACK ===
- ""feedback"" PHẢI bằng TIẾNG VIỆT, tối đa 2 câu ngắn gọn
- Giải thích logic đoạn văn: tại sao câu đó đúng/sai
- Ví dụ tốt: ""Đoạn văn đang so sánh 2 quan điểm đối lập về ảnh hưởng giữa cảm xúc và biểu cảm. ㉠ cần nói ngược lại với câu trước — 'biểu cảm ảnh hưởng lên cảm xúc'. Câu '표정이 감정에 영향을 준다' logic hoàn toàn.""
- KHÔNG viết feedback thuần tiếng Hàn";

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
      ""feedback"": ""<tiếng Việt, tối đa 2 câu — phân tích logic đoạn văn>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do ngắn tiếng Việt>""]
    },
    {
      ""blank_id"": ""㉡"",
      ""user_answer"": ""<câu người dùng>"",
      ""score"": <0-5>,
      ""evaluation"": ""correct|incorrect|partial"",
      ""feedback"": ""<tiếng Việt, tối đa 2 câu — phân tích logic đoạn văn>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do ngắn tiếng Việt>""]
    }
  ]
}

CRITICAL:
- ""suggestions"" chỉ khi evaluation != ""correct""
- Trả về ONLY JSON, KHÔNG markdown
- JSON phải HOÀN CHỈNH
- ""feedback"" tối đa 150 ký tự
- ""suggestions"" tối đa 2 phần tử, mỗi phần tử tối đa 100 ký tự";
    }
}