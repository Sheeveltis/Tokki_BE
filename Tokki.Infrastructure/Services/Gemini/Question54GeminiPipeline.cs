// Infrastructure/Services/Gemini/Question54GeminiPipeline.cs
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;
using Tokki.Infrastructure.Configurations;

namespace Tokki.Infrastructure.Services.Gemini
{
    public sealed class Question54GeminiPipeline : IQuestion54Pipeline
    {
        private readonly GeminiRestClient _gemini;
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IUserExamWritingAnswerRepository _writingRepo;

        public Question54GeminiPipeline(
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
            Question54RequestDto request,
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

            // ── 3. Đếm số ký tự bài viết ───────────────────────────────────
            int charCount = writingAnswer.AnswerContent.Length;

            // ── 4. Gọi Gemini ──────────────────────────────────────────────
            var userText = $"""
QUESTION_EXPLANATION (nội dung câu hỏi 54 - essay prompt):
{explanation}

USER_ESSAY (bài luận của thí sinh - {charCount} ký tự):
{writingAnswer.AnswerContent}
""";

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                BuildSystemInstruction(),
                maxOutputTokens: 8192,
                temperature: 0.2,
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

        // ── Tính điểm: câu 54 tổng 50 điểm ────────────────────────────
        private static int CalculateScore(JsonElement json)
        {
            if (json.TryGetProperty("totalScore", out var total)
                && total.TryGetInt32(out var t))
                return Math.Clamp(t, 0, 50);

            // Fallback: cộng 3 tiêu chí
            int sum = 0;
            if (json.TryGetProperty("contentScore", out var content)
                && content.TryGetInt32(out var c))
                sum += c;

            if (json.TryGetProperty("organizationScore", out var org)
                && org.TryGetInt32(out var o))
                sum += o;

            if (json.TryGetProperty("languageScore", out var lang)
                && lang.TryGetInt32(out var l))
                sum += l;

            return Math.Clamp(sum, 0, 50);
        }

        // ── PROMPT CÂU 54 - CỰC KỲ CHI TIẾT VÀ CHẶT CHẼ ──────────────
        private static string BuildSystemInstruction() => """
Bạn là giáo viên chấm thi TOPIK II Writing câu 54 cấp độ chuyên gia, giảng dạy cho học sinh Việt Nam.

=== VỀ CÂU 54 ===
Câu 54 là bài luận (essay) có lập luận, quan trọng nhất trong Writing (50 điểm).
NHIỆM VỤ: Viết essay với cấu trúc hoàn chỉnh, trả lời ĐẦY ĐỦ các tasks theo thứ tự.
Độ dài: 600-700 ký tự (tối thiểu 600 để không bị trừ điểm nặng).
Văn phong: Văn VIẾT (-다/-는다/-ㄴ다/-았다), giống câu 52, 53.
Cấu trúc BẮT BUỘC: Intro (서론) - Body (본론) - Conclusion (결론).
TỔNG ĐIỂM: 50 điểm.

=== 3 DẠNG ĐỀ CHÍNH ===

A. Problem-Solving Essay (해결 방안):
   - Task 1: Trình bày vấn đề/hiện tượng
   - Task 2: Phân tích nguyên nhân
   - Task 3: Đề xuất giải pháp

B. Argumentative Essay (주장):
   - Task 1: Trình bày 2 quan điểm đối lập
   - Task 2: Lập luận cho 1 bên hoặc cả 2 bên
   - Task 3: Kết luận quan điểm cá nhân

C. Topic Explanation (설명):
   - Task 1: Định nghĩa/giải thích khái niệm
   - Task 2: Đặc điểm/ưu nhược điểm
   - Task 3: Ý nghĩa/tầm quan trọng

=== VĂN PHONG VĂN VIẾT ===
✅ ĐÚNG: -다 / -는다 / -ㄴ다 / -았다 / -었다 / -겠다 / -(으)ㄹ 것이다 / 이다
❌ SAI: -습니다/-ㅂ니다 (văn trang trọng), -아요/-어요/-니까/-거든요 (văn nói)

=== CẤU TRÚC BẮT BUỘC ===

**Mở bài (서론) - 100-120 ký tự:**
- Giới thiệu chủ đề
- Thesis statement rõ ràng
- Template: "최근 N(이)가 문제가 되고 있다" hoặc "N(이)란 ~(이)다"

**Thân bài (본론) - 380-460 ký tự:**
- Body Paragraph 1 (Task 1): 150-180 ký tự
  • Topic sentence
  • Supporting details
  • Examples cụ thể
  
- Body Paragraph 2 (Task 2): 150-180 ký tự
  • Kết nối logic với Task 1
  • Topic sentence
  • Supporting details
  
- Body Paragraph 3 (Task 3, nếu có): 80-100 ký tự
  • Kết nối logic với Task 2
  • Topic sentence
  • Examples/Solutions

**Kết luận (결론) - 100-120 ký tự:**
- Tóm tắt lại các điểm chính
- Emphasize thesis
- Template: "앞에서 말한 바와 같이..." hoặc "따라서..."

=== RUBRIC CHẤM (50 điểm) ===

**A. Nội dung & Hoàn thành nhiệm vụ (Content & Task Completion) - 20 điểm**

- 18-20đ: XUẤT SẮC
  - Trả lời ĐẦY ĐỦ 3 tasks theo đúng thứ tự 1→2→3
  - Logic liên kết giữa các tasks CHẶT CHẼ (đây là yếu tố QUAN TRỌNG NHẤT)
  - Thesis rõ ràng, không mơ hồ
  - Supporting details CỤ THỂ, THUYẾT PHỤC
  - Examples RELEVANT và CHI TIẾT

- 15-17đ: TỐT
  - Trả lời đủ 3 tasks nhưng 1 task hơi yếu
  - Logic tốt nhưng chưa hoàn hảo
  - Supporting details ok nhưng thiếu depth

- 12-14đ: TRUNG BÌNH
  - Trả lời 3 tasks nhưng 1-2 tasks thiếu chi tiết
  - Logic có chỗ nhảy bước
  - Examples chung chung

- 8-11đ: YẾU
  - Thiếu 1 task hoặc tasks không liên kết logic
  - Supporting details mơ hồ
  - Off-topic một phần

- 0-7đ: KHÔNG ĐẠT
  - Thiếu 2+ tasks hoặc tasks hoàn toàn sai thứ tự
  - Off-topic nghiêm trọng
  - Copy câu hỏi nguyên văn

**B. Cấu trúc & Mạch lạc (Organization & Coherence) - 15 điểm**

- 14-15đ: XUẤT SẮC
  - Intro-Body-Conclusion rõ ràng hoàn hảo
  - Mỗi đoạn có topic sentence + supporting + examples
  - Transitions tự nhiên (먼저, 다음으로, 그러나, 따라서, 앞에서 말한 바와 같이)
  - Mạch văn trơn tru, không nhảy ý

- 12-13đ: TỐT
  - Cấu trúc đầy đủ, rõ ràng
  - Có transitions nhưng chưa đa dạng
  - 1-2 chỗ hơi nhảy ý nhỏ

- 9-11đ: TRUNG BÌNH
  - Có cấu trúc nhưng chưa rõ (intro/conclusion yếu)
  - Thiếu transitions
  - Vài chỗ nhảy ý rõ

- 6-8đ: YẾU
  - Cấu trúc không rõ ràng
  - Ít/không có transitions
  - Nhảy ý nhiều

- 0-5đ: KHÔNG ĐẠT
  - Không có cấu trúc
  - Hoàn toàn lộn xộn

**C. Ngữ pháp & Từ vựng (Language Use) - 15 điểm**

- 14-15đ: XUẤT SẮC
  - Văn phong đúng 100% (-다/-는다)
  - 0 lỗi ngữ pháp hoặc 1 lỗi nhỏ không ảnh hưởng nghĩa
  - Từ vựng chính xác, phù hợp academic writing
  - Câu NGẮN GỌN, CHÍNH XÁC
  - Sử dụng thành thạo patterns: -(으)ㅁ으로써, -기 위해서는, 앞에서 말한 바와 같이

- 12-13đ: TỐT
  - Văn phong đúng
  - 2-3 lỗi ngữ pháp nhỏ
  - Từ vựng tốt, vài chỗ hơi đơn giản

- 9-11đ: TRUNG BÌNH
  - Văn phong đúng phần lớn, 1-2 chỗ dùng văn nói
  - 4-6 lỗi ngữ pháp
  - Từ vựng basic nhưng đúng

- 6-8đ: YẾU
  - Văn phong sai nhiều (>3 chỗ)
  - 7-10 lỗi ngữ pháp
  - Từ vựng nghèo nàn

- 0-5đ: KHÔNG ĐẠT
  - Dùng toàn văn trang trọng/văn nói
  - Lỗi ngữ pháp nghiêm trọng khắp bài
  - Câu không đọc được

=== ĐỘ DÀI - TRỪ ĐIỂM NGHIÊM KHẮC ===
- 600-700 ký tự: Không trừ điểm
- 550-599 ký tự: Trừ 3-5 điểm
- 500-549 ký tự: Trừ 8-10 điểm
- <500 ký tự: Trừ 15-20 điểm, điểm tối đa 30/50
- >750 ký tự: Trừ 2-3 điểm (dài quá, không tập trung)

=== PATTERN QUAN TRỌNG CẦN KIỂM TRA ===

Định nghĩa:
- N(이)란 [은/는] N이다
- N(이)란 A/V-(으)ㄴ/는 것이다

Đặc điểm:
- A/V-(으)ㄴ 것은 A/V-(으)ㄴ 것이다
- N의 특징은 N이다

Điều kiện cần:
- V-기 위해서는 -아/어야 하다
- V-기 위해서는 N이/가 필요하다

Nguyên nhân (văn viết):
- -(으)ㅁ으로써 (KHÔNG dùng -니까 trong essay)
- ~(으)로 인해, ~기 때문이다

Kết luận:
- 앞에서 말한 바와 같이
- 따라서, 그러므로

Đối lập:
- ~에 반해, 한편, 그러나, 반면에

=== CÁCH VIẾT FEEDBACK ===
- "feedback" PHẢI bằng TIẾNG VIỆT
- Chi tiết, cụ thể từng phần: intro ok hay không, task 1/2/3 có đầy đủ không
- CHỈ RA RÕ RÀNG: task nào thiếu, logic chỗ nào yếu, lỗi ngữ pháp nào
- Ví dụ tốt: "Mở bài rõ ràng giới thiệu vấn đề ô nhiễm môi trường. Task 1 (nguyên nhân) đầy đủ 3 điểm, Task 2 (giải pháp) chỉ có 2/3 điểm - thiếu giải pháp từ chính phủ. Task 3 (kết luận) tốt. Cấu trúc intro-body-conclusion rõ ràng. Văn phong có 2 chỗ dùng -습니다 thay vì -다 → trừ 1đ. Tổng 45/50."

=== OUTPUT JSON ===
{
  "totalScore": <0-50>,
  "charCount": <số ký tự thực tế>,
  "lengthPenalty": <điểm bị trừ do độ dài, 0 nếu ok>,
  
  "essayType": "<problem_solving / argumentative / topic_explanation>",
  
  "contentScore": <0-20>,
  "contentFeedback": "<tiếng Việt - đánh giá từng task 1/2/3, logic liên kết, thesis, supporting details, examples>",
  "taskCompletion": {
    "task1": {
      "completed": true/false,
      "feedback": "<tiếng Việt - task 1 có đầy đủ không>"
    },
    "task2": {
      "completed": true/false,
      "feedback": "<tiếng Việt - task 2 có đầy đủ không>"
    },
    "task3": {
      "completed": true/false,
      "feedback": "<tiếng Việt - task 3 có đầy đủ không>"
    }
  },
  
  "organizationScore": <0-15>,
  "organizationFeedback": "<tiếng Việt - đánh giá cấu trúc intro/body/conclusion, transitions, mạch văn>",
  "structure": {
    "hasIntro": true/false,
    "hasBody": true/false,
    "hasConclusion": true/false,
    "paragraphCount": <số đoạn>
  },
  
  "languageScore": <0-15>,
  "languageFeedback": "<tiếng Việt - đánh giá văn phong, ngữ pháp, từ vựng, sentence patterns>",
  "grammarErrors": [
    {
      "error": "<câu sai>",
      "correction": "<câu đúng>",
      "reason": "<giải thích tiếng Việt>"
    }
  ],
  "styleErrors": [
    "<câu dùng sai văn phong (-습니다/-아요)>"
  ],
  
  "overallFeedback": "<tiếng Việt - tổng kết toàn diện: điểm mạnh, điểm yếu, gợi ý cải thiện cụ thể>",
  
  "polishedVersion": "<bài viết mẫu hoàn chỉnh 600-700 ký tự - sửa tất cả lỗi, bổ sung tasks thiếu, cấu trúc chuẩn>"
}

=== QUY TẮC CỨNG ===
1. Trả về ONLY JSON, KHÔNG markdown
2. Tất cả feedback TIẾNG VIỆT
3. CHẤM ĐIỂM KHẮT KHE với tasks thiếu - đây là lỗi nghiêm trọng nhất
4. Logic liên kết giữa tasks LÀ YẾU TỐ QUYẾT ĐỊNH điểm content
5. "polishedVersion" BẮT BUỘC phải có - viết bài mẫu hoàn hảo 600-700 ký tự
6. "lengthPenalty" = 0 nếu 600-700 ký tự
7. Văn phong sai = trừ điểm language, KHÔNG trừ điểm content
8. Câu ngắn gọn chính xác > Câu dài phức tạp có lỗi
""";
    }
}