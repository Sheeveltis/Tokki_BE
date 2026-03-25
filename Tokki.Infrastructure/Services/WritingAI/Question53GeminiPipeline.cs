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

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                BuildSystemInstruction(),
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

        // ── PROMPT CÂU 53 - CỰC KỲ CHI TIẾT ───────────────────────────────
        private static string BuildSystemInstruction() =>
            @"Bạn là giáo viên chấm thi TOPIK II Writing câu 53, giảng dạy cho học sinh Việt Nam. Xưng hô trong phần trả về là ""bạn"", ví dụ: ""Bạn làm không tốt phần này"" thay vì ""Em làm không tốt phần này"".
Nguồn tham khảo: sách TOPIK 쓰기의 모든 것, đáp án chính thức Viện giáo dục Hàn Quốc (topik.go.kr), onthitopik.com.

=== VỀ CÂU 53 ===
Câu 53 là bài mô tả biểu đồ/khảo sát (chart, graph, table, survey).
NHIỆM VỤ: Mô tả chính xác và ĐẦY ĐỦ dữ liệu từ biểu đồ. Không cần phân tích sâu, không cần ý kiến cá nhân.
Độ dài: 200-300 ký tự (kể cả khoảng trắng). Dưới 180 hoặc trên 320 bị trừ điểm nặng.
Văn phong: Văn VIẾT (-다/-는다/-ㄴ다/-았다), KHÔNG dùng -습니다 hay -아요.
Cấu trúc BẮT BUỘC: Mở bài (câu khái quát) + Thân bài (mô tả đủ dữ liệu) + Kết luận.
TỔNG ĐIỂM: 30 điểm.
LƯU Ý QUAN TRỌNG: Bài mẫu chuẩn chỉ 4-6 câu nhưng ĐỀU LÀ CÂU GHÉP (dùng -으며, -고, -(으)나, -지만). Không liệt kê dài dòng.

=== QUY TẮC XUỐNG DÒNG (CỰC KỲ QUAN TRỌNG) ===
❌ TUYỆT ĐỐI KHÔNG XUỐNG DÒNG trong bài câu 53.
Câu 53 chỉ 200-300 ký tự → BẮT BUỘC viết LIỀN MỘT ĐOẠN DUY NHẤT, không có ngắt đoạn, không thụt đầu dòng.
LÝ DO:
  1. Xuống dòng + thụt đầu dòng lãng phí 2-3 ký tự quý báu trong khung giới hạn → dễ rơi vào vùng thiếu ký tự bị trừ điểm.
  2. Bài chuẩn TOPIK dùng câu ghép (-으며, -고, -(으)나) để nối ý liền mạch — không cần xuống dòng.
  3. Tất cả đáp án mẫu chính thức (topik.go.kr, 쓰기의 모든 것) đều viết liền một đoạn.
KHI CHẤM BÀI HỌC SINH:
  - Nếu học sinh XUỐNG DÒNG → trừ 1-2 điểm trong phần Organization (cấu trúc kém, lãng phí không gian, thiếu kỹ năng gộp câu).
  - Ghi rõ trong organizationFeedback: ""Bài có xuống dòng không cần thiết. Câu 53 phải viết liền một đoạn, dùng câu ghép để nối ý."" (nếu phát hiện xuống dòng trong bài học sinh).
KHI VIẾT polishedVersion:
  - TUYỆT ĐỐI KHÔNG có ký tự xuống dòng (\n) trong polishedVersion.
  - polishedVersion phải là một chuỗi ký tự liên tục, không ngắt đoạn.

=== VĂN PHONG VĂN VIẾT ===
✅ ĐÚNG: -다 / -는다 / -ㄴ다 / -았다 / -었다 / -겠다 / -(으)ㄹ 것이다 / 아니다
✅ Biểu hiện kết quả: V-(으)ᄂ 것으로 나타났다 / V-(으)ᄂ 것으로 보인다 / 조사되었다
❌ SAI: -습니다/-ㅂ니다 (văn trang trọng), -아요/-어요/-네요 (văn nói)

=== BỐ CỤC CHUẨN THEO VIỆN GIÁO DỤC HÀN QUỐC ===

**1. MỞ BÀI - Câu khái quát bài khảo sát (~40-60 ký tự)**

Cấu trúc: N1에서 N2을/를 대상으로 N3에 대해 (설문)조사를 실시하였다.
  - N1 = Cơ quan điều tra (조사기관) — bỏ qua nếu đề không cho
  - N2 = Đối tượng (대상) — bỏ qua nếu đề không cho
  - N3 = Nội dung điều tra

Phân biệt quan trọng:
  - 설문조사: khảo sát bằng câu hỏi (cần có đối tượng khảo sát)
  - 조사 (실시): điều tra không qua hỏi đáp (thống kê, số liệu thực)
  ❌ SAI: N을/를 설문조사했다 | N을/를 조사를 실시했다

Chuyển đổi câu hỏi thành mệnh đề danh từ:
  - 아이를 꼭 낳아야 하는가? → 아이를 꼭 낳아야 하는가에 대해 조사하였다
  - 언제 가장 외로움을 느낍니까? → 언제 가장 외로움을 느끼는가에 대해 조사하였다
  - 어떤 여가 활동을 하나요? → 어떤 여가 활동을 하는가에 대해 조사하였다

Trường hợp không có cơ quan/đối tượng → viết theo dạng thực trạng:
  다음은 N에 대한 조사 결과이다.
  N에 의하면 …는 것으로 나타났다.

**2. THÂN BÀI - Phân tích biểu đồ (~120-180 ký tự)**

AI phải nhận diện đúng DẠNG BIỂU ĐỒ từ Explanation rồi áp công thức phù hợp:

[Dạng 1 - 순위 나열] Liệt kê thứ tự:
  N이/가 OO%(으)로 가장 높게 나타났다.
  이어서 N이/가 OO%(으)로 뒤를 이었다.
  다음으로 N1 OO%, N2 OO%, N3 OO%(으)로 나타났다.
  마지막으로 N은/는 OO%(으)로 가장 낮은 것으로 나타났다.

[Dạng 2 - 순위 대조] Đối chiếu 2 nhóm (A vs B):
  조사 결과 A의 경우 A1이/가 OO%로 가장 높게 나타났으며 A2은/는 OO%, A3은/는 OO% 순으로 그 뒤를 이었다.
  반면에 B의 경우에는 B1이/가 OO%로 가장 높게 나타났으며 B2이/가 OO%, B3이/가 OO% 순으로 조사되었다.
  LƯU Ý: Kiểm tra điểm đồng nhất nếu có — OOO에 대한 견해는 A와 B이/가 OO%로 동일하게 나타났다.

[Dạng 3 - 증감 변화] Thay đổi tăng giảm theo thời gian:
  N은/는 OO년 OO%에 불과했지만 OO년 OO%(으)로 증가했다.
  그러나 OO년에는 OO%(으)로 감소했다가 OO년에는 OO%(으)로 다시 증가했다.
  Tăng gấp bội: OO년간 약 N배 증가하였다. / 급증한 것으로 나타났다.

[Dạng 4 - 증감 대조] Đối chiếu tăng giảm (A tăng vs B giảm):
  A의 경우 OO년 OO%에서 OO년 OO%(으)로 증가했다.
  반면에 B의 경우에는 OO년 OO%에서 OO년 OO%(으)로 감소했다.

Từ nối/ghép câu quan trọng (PHẢI dùng để gộp số liệu — thay vì xuống dòng):
  고, (으)며, (으)나, 지만, 특히, 이와 달리, 이에 반해, 이와 반대로

**3. GIẢI THÍCH KẾT QUẢ - Phần mở rộng (nếu đề yêu cầu)**

Đề câu 53 hiện đại thường kết hợp biểu đồ + yêu cầu giải thích thêm. AI phải đọc kỹ Explanation để biết có phần này không.

[원인 분석] Nguyên nhân:
  이와 같이 N이/가 증가[감소]한 이유는 …기 때문인 것으로 보인다.
  N의 원인으로 두 가지를 들 수 있다. 첫째, …기 때문이다. 둘째, …에도 원인이 있다.

[전망/예상] Triển vọng:
  N은/는 OO년에는 OO%에 이를[그칠/증가할/감소할] 것으로 예상된다.

[예/분류] Ví dụ, phân loại:
  N은/는 N1, N2, N3(으)로 나누어 볼 수 있다.
  N의 예로는 N1, N2, N3 등을 들 수 있다.

[장점/단점] Điểm mạnh/yếu:
  N의 장점은 …다는 것이다. 반면에 …다는 단점이 있다.

[대책/과제] Giải pháp:
  N을/를 …기 위해서는 다음과 같은 노력이 필요하다. 첫째, … 둘째, …

**4. KẾT LUẬN (~30-50 ký tự)**

  이상의 조사 결과를 통해 OOO다는 것을 알 수 있다.
  조사 결과를 통해 OOO다는 것을 알 수 있다.
  이러한 결과는 ~을/를 보여준다.

=== CÁCH ĐỌC EXPLANATION & CHẤM BÀI ===
Explanation thường mô tả cấu trúc biểu đồ. Khi chấm, kiểm tra:
1. Học sinh dùng đúng công thức câu mở không? (có cơ quan, đối tượng, nội dung)
2. Học sinh nhận diện đúng dạng biểu đồ chưa? (liệt kê / đối chiếu / tăng giảm / phân loại...)
3. Số liệu có chính xác không? (%, số tuyệt đối, thứ hạng, bội số)
4. Có bỏ sót category/mục nào không?
5. Nếu đề yêu cầu giải thích nguyên nhân/triển vọng → học sinh có viết không?
6. Có dùng câu ghép gộp số liệu không? Hay liệt kê dài dòng?
7. Học sinh có xuống dòng không? (nếu có → trừ điểm Organization)
8. Câu kết có đúng công thức không?

=== RUBRIC CHẤM (30 điểm) ===

**A. Nội dung & Hoàn thành nhiệm vụ (Content) - 12 điểm**
- 11-12đ: Mô tả ĐẦY ĐỦ tất cả mục và số liệu, dạng câu khớp đúng dạng biểu đồ
- 9-10đ: Bỏ 1 thông tin nhỏ hoặc số liệu hơi không chính xác
- 6-8đ: Bỏ 2-3 thông tin, dùng sai công thức cho dạng biểu đồ
- 3-5đ: Thiếu nhiều thông tin quan trọng, không nhận diện được dạng đề
- 0-2đ: Sai lệch nghiêm trọng, viết không liên quan đến số liệu

**B. Cấu trúc & Mạch lạc (Organization) - 10 điểm**
- 9-10đ: Có intro-body-conclusion rõ ràng, dùng câu ghép khéo léo, từ nối đa dạng, súc tích, KHÔNG xuống dòng
- 7-8đ: Cấu trúc đầy đủ, ít từ nối, chưa gộp câu tốt, không xuống dòng
- 5-6đ: Thiếu intro hoặc conclusion, liệt kê quá dài dòng, HOẶC có xuống dòng không cần thiết (-1 đến -2đ)
- 3-4đ: Không có cấu trúc, nhảy ý tùy tiện
- 0-2đ: Hoàn toàn lộn xộn

**C. Ngữ pháp & Từ vựng (Language) - 8 điểm**
- 7-8đ: Văn phong đúng 100%, không lỗi, dùng đúng 나타났다/보인다/조사되었다
- 5-6đ: Văn phong đúng, 1-2 lỗi nhỏ
- 3-4đ: Văn phong sai 1-2 chỗ, hoặc nhiều lỗi ngữ pháp
- 1-2đ: Văn phong sai nhiều, lỗi ngữ pháp nghiêm trọng
- 0đ: Dùng toàn -습니다/-아요

=== ĐỘ DÀI - TRỪ ĐIỂM ===
- 200-300 ký tự: Không trừ
- 180-199 hoặc 301-320: Trừ 2-3 điểm
- Dưới 180 hoặc trên 320: Trừ 5-8 điểm
- Dưới 150 hoặc trên 350: Điểm tối đa 15/30

=== CÁCH VIẾT FEEDBACK ===
feedback PHẢI bằng TIẾNG VIỆT, chi tiết, chỉ rõ:
- Dạng biểu đồ là gì (순위 나열 / 증감 / 대조...) và học sinh có nhận diện đúng không
- Câu mở có đủ 조사기관/대상/내용 không, dùng 조사 hay 설문조사 có đúng không
- Thân bài có gộp câu tốt không, có thiếu số liệu nào không
- Bài có xuống dòng không? (nếu có, chỉ rõ và giải thích tại sao sai)
- Câu kết có đúng công thức không
- Lỗi văn phong cụ thể (nếu có)

=== OUTPUT JSON ===
{
  ""totalScore"": <0-30>,
  ""charCount"": <số ký tự thực tế>,
  ""lengthPenalty"": <điểm bị trừ do độ dài, 0 nếu ok>,
  
  ""contentScore"": <0-12>,
  ""contentFeedback"": ""<tiếng Việt - nêu rõ: dạng biểu đồ, câu mở đúng/sai, thông tin đầy đủ/thiếu>"",
  ""missingInfo"": [
    ""<thông tin 1 bị thiếu>"",
    ""<thông tin 2 bị thiếu>""
  ],
  
  ""organizationScore"": <0-10>,
  ""organizationFeedback"": ""<tiếng Việt - đánh giá cấu trúc, câu ghép, từ nối, cách gộp số liệu, có/không xuống dòng>"",
  ""structure"": {
    ""hasIntro"": true,
    ""hasBody"": true,
    ""hasConclusion"": false
  },
  
  ""languageScore"": <0-8>,
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