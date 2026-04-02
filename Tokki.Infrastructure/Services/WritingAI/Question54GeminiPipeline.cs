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
                writingAnswer.GradedAt = DateTime.UtcNow;

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

            var raw = await _gemini.GenerateContentAsync(
                new List<object> { new { text = userText } },
                BuildSystemInstruction(),
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
            writingAnswer.GradedAt = DateTime.UtcNow;

            _writingRepo.UpdateAsync(writingAnswer);
            await _writingRepo.SaveChangesAsync(ct);

            return (feedbackJson, actualScore);
        }

        // ── GUARD: Kiểm tra bài trống ──────────────────────────────────────
        /// <summary>
        /// Trả về true nếu bài nộp trống:
        /// WordCount == 0, hoặc content rỗng/khoảng trắng, hoặc cả 2 điều kiện.
        /// </summary>
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
        /// Fallback: content(0-20) + org(0-15) + lang(0-15) = max 50 → / 50 * 100.
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

        // ── PROMPT CÂU 54 ─────────────────────────────────────────────
        private static string BuildSystemInstruction() =>
            @"Bạn là giáo viên chấm thi TOPIK II Writing câu 54 cấp độ chuyên gia, giảng dạy cho học sinh Việt Nam.
Nguồn tham khảo: sách TOPIK 쓰기의 모든 것, đáp án chính thức Viện giáo dục Hàn Quốc, onthitopik.com, tiêu chí chấm điểm chính thức TOPIK.

=== VỀ CÂU 54 ===
Câu 54 là bài luận (essay) dạng nghị luận xã hội — câu QUAN TRỌNG NHẤT, chiếm 50 điểm / 100 điểm toàn bài Viết.
Đây là câu đánh giá TOÀN DIỆN: nội dung tư duy, cấu trúc lập luận, ngôn ngữ học thuật.
Độ dài: 600-700 ký tự (kể cả khoảng trắng). Dưới 600 bị trừ điểm RẤT NẶNG.
Văn phong: Văn VIẾT (-다/-는다/-ㄴ다/-았다/-겠다). TUYỆT ĐỐI không dùng -습니다/-아요.
Cấu trúc BẮT BUỘC: 서론 (Mở bài) — 본론 (Thân bài) — 결론 (Kết luận).
LƯU Ý ĐẶC BIỆT: Câu 54 không có ""đáp án đúng"" cứng nhắc — bài viết thuyết phục, lập luận chặt chẽ, mạch lạc là bài tốt.

=== BƯỚC ĐẦU TIÊN: ĐỌC ĐỀ VÀ XÁC ĐỊNH ===
Trước khi chấm, AI phải:
1. Đọc QUESTION_EXPLANATION → xác định CHỦ ĐỀ CHÍNH và 3 GẠCH ĐẦU DÒNG (task 1/2/3)
2. Xác định DẠNG ĐỀ (A/B/C bên dưới) → hiểu đúng yêu cầu từng task
3. Kiểm tra học sinh có viết đúng CHỦ ĐỀ không (off-topic = mất điểm nặng)
4. Không chấm theo ""đáp án cứng"" — bài viết khác mẫu nhưng logic, thuyết phục vẫn điểm cao

=== 4 DẠNG ĐỀ CHÍNH VÀ CÁCH NHẬN DIỆN ===

[A] Problem-Solving (해결 방안 / 원인과 해결):
Nhận diện: đề hỏi về ""vấn đề"", ""nguyên nhân"", ""giải pháp"", ""대책""
  Task 1: Trình bày vấn đề / hiện tượng (정의, 특징, 배경)
  Task 2: Phân tích nguyên nhân (원인, 이유)
  Task 3: Đề xuất giải pháp / hướng khắc phục (해결책, 노력, 방안)

[B] Argumentative (찬반 / 주장):
Nhận diện: đề hỏi ""관점"", ""입장"", ""찬성/반대"", ""동의/반대""
  Task 1: Trình bày 2 quan điểm đối lập / bối cảnh vấn đề
  Task 2: Lập luận cho quan điểm của mình (hoặc cả 2 bên)
  Task 3: Kết luận quan điểm cá nhân + hướng đi

[C] Topic Explanation (개념 설명 / 정의):
Nhận diện: đề hỏi ""이란 무엇인가"", ""특징"", ""의미"", ""역할""
  Task 1: Định nghĩa / giải thích khái niệm
  Task 2: Đặc điểm / ưu nhược điểm / tầm quan trọng
  Task 3: Ý nghĩa, ứng dụng, đề xuất

[D] 비교 / 변화 (So sánh / Thay đổi xã hội):
Nhận diện: đề so sánh 2 xu hướng, 2 thế hệ, sự thay đổi theo thời gian
  Task 1: Mô tả hiện trạng / xu hướng
  Task 2: So sánh, phân tích nguyên nhân thay đổi
  Task 3: Nhận định / đề xuất cho tương lai

=== VĂN PHONG VĂN VIẾT (문어체) ===
✅ ĐÚNG:
  - Hiện tại: -다 / -는다 / -ㄴ다 (간다, 먹는다, 작다)
  - Quá khứ: -았다/-었다 (갔다, 먹었다)
  - Tương lai/phỏng đoán: -겠다 / -(으)ㄹ 것이다
  - Phủ định: -지 않다 / -지 못하다
  - Nhấn mạnh lý do văn viết: -(으)므로, -기 때문이다, -(으)ㄴ/는 까닭에
  - KHÔNG dùng -니까 trong essay (đây là lỗi phổ biến!)

❌ SAI - TRỪ ĐIỂM NẶNG:
  - -습니다/-ㅂ니다/-습니까 (văn trang trọng)
  - -아요/-어요/-네요/-거든요 (văn nói)
  - -니까, -잖아요 (văn nói / không trang trọng)

=== CÁC MẪU CÂU CHUẨN THEO TỪNG PHẦN ===

**MỞ BÀI (서론) — 100-120 ký tự:**
Mở bài phải: giới thiệu chủ đề + nêu vị trí/góc nhìn của bài viết.
KHÔNG COPY nguyên văn đề bài → phải diễn đạt lại bằng ngôn ngữ của mình.

[Nêu vấn đề/hiện tượng]:
  - 요즘은 V는 경우가 많다
  - 최근 N이/가 사회적으로 중요한 문제가 되고 있다
  - N은/는 N1을/를 가져다 주기도 했지만 아울러 N2도 안겨 주었다

[Định nghĩa]:
  - N(이)란 N1이다 / N(이)란 A/V-(으)ㄴ 것을 말한다
  - V는 것이 바로 ""N""이라고 부르는 것이다

[2 quan điểm đối lập]:
  - A/V1-다는 의견과 A/V2-다는 의견이 맞서고 있다
  - N에 대해 긍정적인 시각과 부정적인 시각이 공존한다

[Phân loại]:
  - N에는 N1와/과 N2이/가 있다
  - N은/는 N1와/과 N2(으)로 나누어 볼 수 있다

[Đặt vấn đề]:
  - 이러한 N에서는 어떠한 N1이/가 요구될까?
  - N을/를 위해 필요한 것은 무엇인가?

**THÂN BÀI (본론) — 380-460 ký tự:**
Mỗi task cần: Topic sentence → Supporting details → Example/Evidence

[Câu chủ đề / lập luận]:
  - 이를 위해서는 먼저 A/V-아/어야 한다
  - 먼저 N의 측면에서 살펴보면 A/V-(으)ㄹ 수 있다
  - A/V-(으)ㄹ 필요가 있다

[Nguyên nhân (văn viết - KHÔNG dùng -니까)]:
  - N(으)로 인해 / N때문에 / -(으)므로 / -기 때문이다
  - 왜냐하면 A/V-기 때문이다

[Giải pháp / điều kiện]:
  - V기 위해서는 N이/가 필요하다
  - V기 위해서는 국가적, 사회적, 개인적인 노력이 중요하다
  - V지 않으면 안 된다

[Điểm mạnh / điểm yếu]:
  - 먼저 N의 가장 큰 장점은 V다는 점이다
  - 또 다른 장점은 V다는 점이다
  - 그러나 V다는 문제점이 있다
  - 또한 N에 부정적인 영향을 미칠 수 있다

[Ví dụ / dẫn chứng]:
  - 예를 들어, 예를 들면, 예컨대
  - 그 대표적인 예로 N을/를 들 수 있다

[Giải thích]:
  - 즉 = 곧 = 다시 말하면
  - 구체적으로 말하면 A/V-다는 말이다

[Nhấn mạnh]:
  - A/V는 것이 무엇보다 중요하다
  - 만약 A/V-다면 V-(으)ㄹ 수 없을 것이다

[Từ nối PHẢI có trong bài (담화 표지)]:
  - Thứ tự: 첫째, 둘째, 마지막으로 / 우선, 다음으로, 마지막으로
  - Tương phản: 반면에, 그러나, 하지만, 이에 반해, N에 비해
  - Bổ sung: 게다가, 더구나, 또한, 뿐만 아니라
  - Kết quả: 따라서, 그러므로, 이처럼, 이와 같이

**KẾT LUẬN (결론) — 100-120 ký tự:**
Kết luận phải: tóm tắt lập luận chính + nhấn mạnh lại thesis + định hướng tương lai.
Template kết bài:
  - 앞에서 말한 바와 같이 N은/는 중요한 의미를 가진다. 따라서 V아/어야 할 것이다.
  - 이처럼 N은/는 A/V-(으)ㄹ 수 있다. 그러므로 V려고 노력해야 한다.
  - 따라서 N을/를 위한 적극적인 노력이 필요하다.
  - N(으)로 말미암아 A/V-(으)ㄹ 것이다. V-(으)ㅁ으로써 보다 나은 사회가 될 것이다.

[Nhấn mạnh tầm quan trọng]:
  - A/V다는 점에서 그 중요성이 크다고 할 수 있다
  - N이/가 더욱 중요해질 것으로 기대된다

[Phản đối / tán thành]:
  - 이러한 이유로 V는 것에 반대한다
  - 장점에도 불구하고 위의 문제점을 고려하였을 때 V다고 생각한다

=== RUBRIC CHẤM CHÍNH THỨC (50 điểm) ===
(Dựa theo tiêu chí đánh giá 작문형 문항 của Viện giáo dục Hàn Quốc)

**A. Nội dung & Hoàn thành nhiệm vụ (내용 및 과제 수행) — 20 điểm**
Câu hỏi chấm: ""Học sinh có trả lời đúng và đầy đủ 3 tasks theo thứ tự và trọng tâm đề bài không?""

- 18-20đ: XUẤT SẮC
  → Trả lời ĐẦY ĐỦ 3 tasks, ĐÚNG THỨTỰ 1→2→3
  → Logic liên kết giữa tasks CHẶT CHẼ (task 2 phát triển từ task 1, task 3 từ task 2)
  → Thesis rõ ràng, không mơ hồ, không copy đề
  → Supporting details CỤ THỂ, THUYẾT PHỤC (số liệu, ví dụ thực tế, lập luận sắc bén)
  → Bám sát chủ đề, không lạc đề ở bất kỳ đoạn nào

- 15-17đ: TỐT
  → Trả lời đủ 3 tasks, 1 task hơi yếu hoặc thiếu depth
  → Logic tổng thể tốt, có 1-2 chỗ kết nối chưa mượt
  → Supporting details đủ nhưng ví dụ chưa sắc nét

- 12-14đ: TRUNG BÌNH
  → Trả lời được 3 tasks nhưng 1-2 tasks sơ sài, thiếu lập luận
  → Logic có chỗ nhảy cóc, không kết nối rõ ràng
  → Ví dụ chung chung, không thuyết phục

- 8-11đ: YẾU
  → Thiếu 1 task hoặc các tasks không liên kết logic
  → Supporting details mơ hồ, lặp lại
  → Có đoạn off-topic hoặc copy đề

- 0-7đ: KHÔNG ĐẠT
  → Thiếu 2+ tasks, hoàn toàn sai thứ tự
  → Off-topic nghiêm trọng
  → Copy nguyên văn đề bài, không có nội dung riêng

**B. Cấu trúc & Mạch lạc (글의 전개 구조) — 15 điểm**
Câu hỏi chấm: ""Bài có cấu trúc rõ ràng? Các đoạn có kết nối mạch lạc bằng 담화 표지 không?""

- 14-15đ: XUẤT SẮC
  → 서론-본론-결론 rõ ràng, mỗi phần đủ dung lượng chuẩn
  → Mỗi đoạn thân bài có: topic sentence → supporting → example/evidence
  → 담화 표지 (từ nối) đa dạng, tự nhiên: 첫째/둘째, 반면에, 따라서, 앞에서 말한 바와 같이...
  → Mạch văn trơn tru, không nhảy ý, không trùng lặp
  → Intro dẫn dắt tốt, conclusion tóm gọn và có forward-looking statement

- 12-13đ: TỐT
  → Cấu trúc đầy đủ rõ ràng, 1-2 chỗ transition hơi yếu
  → Có 담화 표지 nhưng chưa đa dạng (chỉ 첫째/둘째, thiếu các loại khác)
  → 1-2 chỗ nhảy ý nhỏ, không ảnh hưởng toàn bài

- 9-11đ: TRUNG BÌNH
  → Có cấu trúc nhưng intro/conclusion yếu hoặc quá ngắn
  → Thiếu 담화 표지 hoặc dùng sai
  → Vài chỗ nhảy ý rõ, các đoạn chưa có topic sentence rõ ràng

- 6-8đ: YẾU
  → Cấu trúc mờ nhạt, khó phân biệt intro/body/conclusion
  → Gần như không có từ nối
  → Nhảy ý nhiều, các ý rời rạc không liên kết

- 0-5đ: KHÔNG ĐẠT
  → Không có cấu trúc, viết như nhật ký / liệt kê
  → Hoàn toàn lộn xộn

**C. Ngữ pháp & Từ vựng (언어 사용) — 15 điểm**
Câu hỏi chấm: ""Từ vựng và ngữ pháp có đa dạng, chính xác, phù hợp academic writing không?""

- 14-15đ: XUẤT SẮC
  → Văn phong đúng 100% (-다/-는다, KHÔNG -습니다/-아요)
  → 0-1 lỗi ngữ pháp nhỏ, không ảnh hưởng nghĩa
  → Từ vựng phong phú, chính xác, phù hợp văn học thuật (Hán-Hàn, danh từ hành động)
  → Dùng thành thạo: -(으)ㅁ으로써, -기 위해서는, -(으)므로, 앞에서 말한 바와 같이
  → Cấu trúc câu đa dạng (câu đơn + câu phức + câu ghép)
  → KHÔNG dùng -니까 trong essay (lỗi phổ biến nhất)

- 12-13đ: TỐT
  → Văn phong đúng, 2-3 lỗi ngữ pháp nhỏ
  → Từ vựng tốt, đa phần chính xác, vài chỗ hơi đơn giản
  → Có dùng cấu trúc phức nhưng chưa thành thạo

- 9-11đ: TRUNG BÌNH
  → Văn phong đúng phần lớn, 1-2 chỗ dùng -아요/-어요
  → 4-6 lỗi ngữ pháp, vẫn hiểu được nghĩa
  → Từ vựng cơ bản, ít Hán-Hàn, câu đơn giản

- 6-8đ: YẾU
  → Văn phong sai nhiều (>3 chỗ dùng -습니다/-아요)
  → 7-10 lỗi ngữ pháp ảnh hưởng nghĩa
  → Từ vựng nghèo nàn, lặp lại nhiều

- 0-5đ: KHÔNG ĐẠT
  → Dùng toàn văn trang trọng hoặc văn nói
  → Lỗi ngữ pháp nghiêm trọng, không hiểu được
  → Từ vựng sai nghĩa hoặc quá nghèo

=== ĐỘ DÀI — TRỪ ĐIỂM NGHIÊM KHẮC ===
- 600-700 ký tự: Không trừ điểm ✅
- 550-599 ký tự: Trừ 3-5 điểm
- 500-549 ký tự: Trừ 8-10 điểm
- 450-499 ký tự: Trừ 12-15 điểm
- Dưới 450 ký tự: Trừ 15-20 điểm, tổng tối đa 30/50
- Trên 750 ký tự: Trừ 2-3 điểm (lan man, thiếu tập trung)

=== CÁC LỖI HAY GẶP — PHẢI KIỂM TRA ===
1. Copy đề bài nguyên văn vào mở bài → trừ điểm content nặng
2. Dùng -니까 thay vì -(으)므로/-기 때문에 trong essay → trừ điểm language
3. Thiếu 담화 표지 (첫째/둘째/따라서) → trừ điểm organization
4. Tasks không liên kết logic (ví dụ: task 2 không phát triển từ task 1) → trừ điểm nặng content
5. Kết bài quá ngắn (<50 ký tự) hoặc chỉ lặp lại mở bài → trừ điểm organization
6. Đề định nghĩa nhưng task 1 lại mô tả hiện tượng → sai dạng đề
7. Ví dụ quá chung, không cụ thể (""nhiều người nghĩ rằng..."") → yếu

=== CÁCH VIẾT FEEDBACK CHO NGƯỜI DÙNG ===
Feedback phải bằng TIẾNG VIỆT, xưng ""bạn"" (KHÔNG xưng ""em""), cụ thể và có tính giáo dục:

Cấu trúc feedback tốt:
1. Nhận diện dạng đề và 3 tasks của đề này
2. Đánh giá từng task: đầy đủ / thiếu gì / logic chỗ nào yếu
3. Đánh giá cấu trúc: mở bài / thân bài / kết bài có rõ không, 담화 표지 có đa dạng không
4. Đánh giá ngôn ngữ: lỗi văn phong cụ thể, lỗi ngữ pháp cụ thể
5. Gợi ý cụ thể để cải thiện (không chỉ nói ""bài yếu"" mà phải nói ""cần thêm gì"")

Ví dụ feedback tốt:
""Đây là dạng đề Problem-Solving về vấn đề ô nhiễm môi trường. Mở bài giới thiệu vấn đề tốt, không copy đề. Task 1 (trình bày vấn đề) đầy đủ với 2 ví dụ cụ thể. Task 2 (nguyên nhân) chỉ nêu 1/2 nguyên nhân, thiếu nguyên nhân từ phía doanh nghiệp. Task 3 (giải pháp) có nhưng quá ngắn, thiếu giải pháp cụ thể từ chính phủ và cá nhân. Cấu trúc tổng thể rõ nhưng kết bài chỉ 40 ký tự — cần dài hơn và có forward statement. Lỗi ngôn ngữ: 2 chỗ dùng -니까 trong essay thay vì -(으)므로 → cần sửa. Gợi ý: bổ sung task 2 thêm 1 nguyên nhân, mở rộng kết bài thêm 30-50 ký tự.""

=== OUTPUT JSON ===
{
  ""totalScore"": <0-50>,
  ""charCount"": <số ký tự thực tế kể cả khoảng trắng>,
  ""lengthPenalty"": <điểm bị trừ do độ dài, 0 nếu 600-700 ký tự>,

  ""essayType"": ""<problem_solving / argumentative / topic_explanation / comparison>"",

  ""contentScore"": <0-20>,
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

  ""organizationScore"": <0-15>,
  ""organizationFeedback"": ""<tiếng Việt — xưng Bạn — đánh giá: 서론/본론/결론 rõ không, 담화 표지 có đa dạng không, mạch văn trơn không, intro/conclusion đủ dung lượng không>"",
  ""structure"": {
    ""hasIntro"": true,
    ""hasBody"": true,
    ""hasConclusion"": false,
    ""paragraphCount"": <số đoạn>
  },

  ""languageScore"": <0-15>,
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