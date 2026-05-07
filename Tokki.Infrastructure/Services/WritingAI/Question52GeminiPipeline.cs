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

            string content = writingAnswer.AnswerContent ?? "";
            string answer1 = "", answer2 = "";

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
                    string cleanedText = part.Trim().TrimStart(':', '：').Trim();
                    
                    if (currentMarker == "㉠") answer1 = cleanedText;
                    else if (currentMarker == "㉡") answer2 = cleanedText;
                }
            }

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
""";

            string? dbConfigJson = await _systemConfigRepo.GetValueByKeyAsync("AI_WRITING_52_PROMPT");
            var promptConfig = new Writing52AiPromptConfigDto();

            if (!string.IsNullOrEmpty(dbConfigJson))
            {
                try
                {
                    var parsedConfig = JsonSerializer.Deserialize<Writing52AiPromptConfigDto>(dbConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedConfig != null) promptConfig = parsedConfig;
                }
                catch (Exception) { }
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

        private static bool IsEmptySubmission(int wordCount) => wordCount <= 5;

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

        private static double CalculatePercentageScore(JsonElement json)
        {
            if (json.TryGetProperty("totalScore", out var total) && total.TryGetDouble(out var t))
                return Math.Clamp(t / 10.0 * 100.0, 0, 100);

            double sum = 0;
            if (json.TryGetProperty("results", out var results))
                foreach (var item in results.EnumerateArray())
                    if (item.TryGetProperty("score", out var s) && s.TryGetDouble(out var v))
                        sum += v;

            return Math.Clamp(sum / 10.0 * 100.0, 0, 100);
        }

        private static int CalculateActualScore(double percentageScore, double maxMark)
        {
            double actual = (percentageScore / 100.0) * maxMark;
            return (int)Math.Round(actual, MidpointRounding.AwayFromZero);
        }

        private static string BuildSystemInstruction(Writing52AiPromptConfigDto config) =>
            $@"{config.Persona}

=== VỀ CÂU 52 ===
{config.QuestionOverview}

=== BAREM CHẤM ĐIỂM CHI TIẾT (Tối đa 5 điểm/chỗ trống) ===
Dựa trên tiêu chuẩn chấm thi, hãy đánh giá bài làm của học sinh qua 3 tiêu chí sau:

1. Nội dung & Ngữ cảnh ({config.ContentContext.MaxScore}đ):
{config.ContentContext.Description}

2. Từ vựng & Ngữ pháp ({config.VocabGrammar.MaxScore}đ):
{config.VocabGrammar.Description}

3. Hình thức & Quy tắc ({config.FormRules.MaxScore}đ):
{config.FormRules.Description}

=== QUY TẮC VĂN PHONG VÀ ĐUÔI CÂU ===
{config.WritingStyleRules}

=== QUY TẮC DẤU CÂU (PUNCTUATION RULE) ===
{config.PunctuationRules}

=== CÁCH VIẾT FEEDBACK ===
{config.FeedbackRequirements}

QUAN TRỌNG: 
- Nếu phát hiện dùng sai đuôi câu (như -ㅂ/습니다 hoặc -아요/어요), hãy đánh giá là 'incorrect' hoặc 'partial' và trừ điểm nặng ở phần Hình thức.
- Nếu ghi thêm dấu câu ở cuối (như '.', '?'), trừ 1 điểm ở phần Hình thức.
- Bài làm của học sinh cho mỗi ô trống CHỈ ĐƯỢC PHÉP là 1 câu duy nhất.";

        private static string BuildOutputSchema() =>
            @"=== OUTPUT JSON SCHEMA BẮT BUỘC ===
Bạn BẮT BUỘC phải trả về đúng cấu trúc JSON dưới đây. CHỈ TRẢ VỀ JSON.

{
  ""totalScore"": <tổng 2 blank, 0-10>,
  ""results"": [
    {
      ""blank_id"": ""㉠"",
      ""user_answer"": ""<câu người dùng>"",
      ""score"": <0-5>,
      ""evaluation"": ""correct|incorrect|partial"",
      ""feedback"": ""<tiếng Việt, tối đa 4 câu>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do ngắn tiếng Việt>""]
    },
    {
      ""blank_id"": ""㉡"",
      ""user_answer"": ""<câu người dùng>"",
      ""score"": <0-5>,
      ""evaluation"": ""correct|incorrect|partial"",
      ""feedback"": ""<tiếng Việt, tối đa 4 câu>"",
      ""suggestions"": [""<tiếng Hàn> — <lý do ngắn tiếng Việt>""]
    }
  ]
}";
    }
}