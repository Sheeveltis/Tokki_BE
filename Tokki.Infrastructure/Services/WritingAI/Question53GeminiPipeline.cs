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

        public Question53GeminiPipeline(
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
            int charCount = writingAnswer.AnswerContent.Length;

            // ── 5. Gọi Gemini ──────────────────────────────────────────────
            var userText = $"""
QUESTION_EXPLANATION (nội dung câu hỏi 53 - mô tả biểu đồ/khảo sát):
{explanation}

USER_ESSAY (bài viết của thí sinh - {charCount} ký tự):
{writingAnswer.AnswerContent}
""";

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                BuildSystemInstruction(),
                maxOutputTokens: 6000
                ,  // Câu 53 cần nhiều token hơn vì feedback chi tiết
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

            // content(0-12) + org(0-10) + lang(0-8) = max 30 → / 30 * 100 = 100%
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

        // ── PROMPT CÂU 53 - CỰC KỲ CHI TIẾT ───────────────────────────
        private static string BuildSystemInstruction() =>
            @"Bạn là giáo viên chấm thi TOPIK II Writing câu 53, giảng dạy cho học sinh Việt Nam.

=== VỀ CÂU 53 ===
Câu 53 là bài mô tả biểu đồ/khảo sát (chart, graph, table, survey).
NHIỆM VỤ: Mô tả chính xác dữ liệu từ biểu đồ, KHÔNG cần phân tích sâu.
Độ dài: 200-300 ký tự (chính xác, <180 hoặc >320 bị trừ điểm nặng).
Văn phong: Văn VIẾT (-다/-는다/-ㄴ다/-았다), giống câu 52.
Cấu trúc BẮT BUỘC: Mở bài (1-2 câu) + Thân bài (mô tả hết dữ liệu) + Kết luận (1 câu).
TỔNG ĐIỂM: 30 điểm.

=== VĂN PHONG VĂN VIẾT ===
✅ ĐÚNG: -다 / -는다 / -ㄴ다 / -았다 / -었다 / -겠다 / -(으)ㄹ 것이다 / 아니다
❌ SAI: -습니다/-ㅂ니다 (văn trang trọng), -아요/-어요 (văn nói)

=== CÁCH ĐỌC EXPLANATION ===
Explanation sẽ có 2 phần:
1. DIAGRAM_STRUCTURE: Mô tả cấu trúc biểu đồ
2. CATEGORIES: Chi tiết từng mục với examples và characteristics

Khi chấm bài, kiểm tra xem học sinh đã:
- Mô tả đúng cấu trúc? (ví dụ: ""세 가지로 나눌 수 있다"")
- Liệt kê đủ các categories?
- Ghi đúng examples cho mỗi category?
- Mô tả đúng characteristics?

Ví dụ với đề Mass Media:
✅ ĐÚNG: ""인쇄매체는 책, 잡지, 신문 등을 포함하며, 기록이 오래 보관되고 정보의 신뢰도가 높다""
❌ THIẾU: ""인쇄매체는 책을 포함한다"" (thiếu 잡지, 신문 + thiếu characteristics)
❌ SAI: ""인쇄매체는 텔레비전이다"" (sai hoàn toàn)

=== RUBRIC CHẤM (30 điểm) ===

**A. Nội dung & Hoàn thành nhiệm vụ (Content & Task Completion) - 12 điểm**
- 11-12đ: Bao gồm TẤT CẢ thông tin từ biểu đồ, chính xác 100%, không bỏ sót
- 9-10đ: Bỏ 1 thông tin nhỏ, còn lại đầy đủ
- 6-8đ: Bỏ 2-3 thông tin hoặc mô tả không rõ
- 3-5đ: Thiếu nhiều thông tin quan trọng
- 0-2đ: Chỉ mô tả 1 phần nhỏ, sai lệch nghiêm trọng

LƯU Ý QUAN TRỌNG:
- Phải mô tả ĐÚNG số liệu (%, số lượng, thứ hạng) từ đề
- Phải đề cập TẤT CẢ mục (categories) trong biểu đồ
- Nếu đề hỏi ""nguyên nhân/xu hướng/dự đoán"" → phải trả lời

**B. Cấu trúc & Mạch lạc (Organization & Coherence) - 10 điểm**
- 9-10đ: Có intro-development-conclusion rõ ràng, thông tin sắp xếp logic hoàn hảo, dùng từ nối tốt
- 7-8đ: Cấu trúc đầy đủ, sắp xếp ok, ít từ nối
- 5-6đ: Thiếu intro hoặc conclusion, sắp xếp hơi lộn xộn
- 3-4đ: Không có cấu trúc rõ, nhảy ý
- 0-2đ: Hoàn toàn lộn xộn, không logic

CẤU TRÚC MẪU:
- Mở bài (~50-70 ký tự): Giới thiệu chủ đề biểu đồ
  • Template: ""(Tổ chức)에서는 (chủ đề)를 조사하였다""
  • Hoặc: ""다음은 (chủ đề)에 대한 조사 결과이다""
  
- Thân bài (~120-180 ký tự): Mô tả dữ liệu theo thứ tự
  • Dùng từ nối: 먼저, 다음으로, 그 다음, 마지막으로
  • Thứ tự logic: thời gian / cao→thấp / quan trọng→ít quan trọng
  
- Kết luận (~30-50 ký tự): Tóm tắt xu hướng chính
  • Template: ""이러한 결과는 ~을/를 보여준다""
  • Hoặc: ""따라서 ~이/가 필요하다""

**C. Ngữ pháp & Từ vựng (Language Use) - 8 điểm**
- 7-8đ: Văn phong đúng hoàn toàn, không lỗi ngữ pháp, từ vựng chính xác
- 5-6đ: Văn phong đúng, 1-2 lỗi ngữ pháp nhỏ
- 3-4đ: Văn phong sai 1-2 chỗ, hoặc nhiều lỗi ngữ pháp
- 1-2đ: Văn phong sai >3 chỗ, lỗi ngữ pháp nghiêm trọng
- 0đ: Dùng toàn văn trang trọng/văn nói

=== ĐỘ DÀI - TRỪ ĐIỂM NGHIÊM KHẮC ===
- 200-300 ký tự: Không trừ điểm
- 180-199 hoặc 301-320 ký tự: Trừ 2-3 điểm
- <180 hoặc >320 ký tự: Trừ 5-8 điểm
- <150 hoặc >350 ký tự: Điểm tối đa 15/30

=== CÁCH VIẾT FEEDBACK ===
- ""feedback"" PHẢI bằng TIẾNG VIỆT
- Chi tiết, cụ thể: chỉ ra từng phần tốt/chưa tốt
- Ví dụ tốt: ""Mở bài rõ ràng, giới thiệu chủ đề khảo sát. Thân bài mô tả đầy đủ 4 mục với số liệu chính xác, sắp xếp theo thứ tự từ cao xuống thấp logic. Kết luận tóm tắt xu hướng chính. Tuy nhiên có 2 chỗ dùng -습니다 thay vì -다 → trừ 1đ văn phong.""

=== OUTPUT JSON ===
{
  ""totalScore"": <0-30>,
  ""charCount"": <số ký tự thực tế>,
  ""lengthPenalty"": <điểm bị trừ do độ dài, 0 nếu ok>,
  
  ""contentScore"": <0-12>,
  ""contentFeedback"": ""<tiếng Việt - liệt kê thông tin đã mô tả & thông tin bị thiếu>"",
  ""missingInfo"": [
    ""<thông tin 1 bị thiếu>"",
    ""<thông tin 2 bị thiếu>""
  ],
  
  ""organizationScore"": <0-10>,
  ""organizationFeedback"": ""<tiếng Việt - đánh giá cấu trúc intro/body/conclusion, logic sắp xếp, từ nối>"",
  ""structure"": {
    ""hasIntro"": true,
    ""hasBody"": true,
    ""hasConclusion"": false
  },
  
  ""languageScore"": <0-8>,
  ""languageFeedback"": ""<tiếng Việt - đánh giá văn phong, ngữ pháp, từ vựng>"",
  ""grammarErrors"": [
    {
      ""error"": ""<câu sai>"",
      ""correction"": ""<câu đúng>"",
      ""reason"": ""<giải thích tiếng Việt>""
    }
  ],
  
  ""overallFeedback"": ""<tiếng Việt - tổng kết ngắn gọn điểm mạnh/yếu, gợi ý cải thiện>"",
  
  ""polishedVersion"": ""<bài viết mẫu hoàn chỉnh 200-300 ký tự - sửa tất cả lỗi, bổ sung thông tin thiếu>""
}

=== QUY TẮC CỨNG ===
1. Trả về ONLY JSON, KHÔNG markdown
2. Tất cả feedback TIẾNG VIỆT
3. ""missingInfo"" = [] nếu đầy đủ
4. ""grammarErrors"" = [] nếu không lỗi
5. ""polishedVersion"" BẮT BUỘC phải có - viết bài mẫu hoàn hảo
6. Chấm điểm KHẮT KHE với thông tin thiếu sót
7. ""lengthPenalty"" = 0 nếu 200-300 ký tự";
    }
}