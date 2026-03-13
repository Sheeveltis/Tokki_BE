//using System.Text.Json;
//using Tokki.Application.IServices;
//using Tokki.Application.UseCases.TopikWriting.DTOs;

//namespace Tokki.Infrastructure.Services.Gemini
//{
//    public sealed class TopikWritingGeminiPipeline : ITopikWritingGeminiPipeline
//    {
//        private readonly GeminiRestClient _gemini;

//        public TopikWritingGeminiPipeline(GeminiRestClient gemini)
//        {
//            _gemini = gemini;
//        }

//        public async Task<JsonElement> SolveAsync(
//            TopikWritingRequestDto request,
//            CancellationToken ct)
//        {
//            // ===== Build parts (text + images) =====
//            var baseParts = new List<object>();

//            var promptText = request.Question?.Prompt?.Text ?? "";
//            baseParts.Add(new
//            {
//                text = $"""
//INPUT_JSON:
//{JsonSerializer.Serialize(request)}

//PROMPT_TEXT:
//{promptText}
//"""
//            });

//            // Download ảnh (nếu có)
//            if (request.Question?.Prompt?.Images is { Count: > 0 } imgs)
//            {
//                foreach (var img in imgs)
//                {
//                    if (string.IsNullOrWhiteSpace(img.Url)) continue;

//                    try
//                    {
//                        var (base64Data, mimeType) = await _gemini.DownloadImageWithMimeAsync(img.Url, ct);

//                        baseParts.Add(new
//                        {
//                            inline_data = new
//                            {
//                                mime_type = mimeType,
//                                data = base64Data
//                            }
//                        });
//                    }
//                    catch (Exception ex)
//                    {
//                        throw new InvalidOperationException($"Không thể tải ảnh từ {img.Url}", ex);
//                    }
//                }
//            }

//            // ✅ Xác định dạng bài dựa vào số câu
//            var questionType = DetermineQuestionType(request.Question.No);
//            var requirements = GetRequirements(questionType);
//            var gradingCriteria = GetGradingCriteria(questionType);

//            // ===== GENERATE SYSTEM PROMPT BASED ON QUESTION TYPE =====
//            var systemPrompt = GenerateSystemPrompt(questionType, requirements, gradingCriteria);

//            var feedbackText = await _gemini.GenerateContentAsync(
//                parts: baseParts,
//                systemInstruction: systemPrompt,
//                maxOutputTokens: 8192,
//                temperature: 0.3,
//                ct: ct);

//            Console.WriteLine("========== RAW GEMINI RESPONSE ==========");
//            Console.WriteLine($"Length: {feedbackText.Length} characters");
//            Console.WriteLine($"First 200 chars: {feedbackText.Substring(0, Math.Min(200, feedbackText.Length))}");
//            Console.WriteLine($"Last 200 chars: {feedbackText.Substring(Math.Max(0, feedbackText.Length - 200))}");
//            Console.WriteLine($"Full response:\n{feedbackText}");
//            Console.WriteLine("========== END RESPONSE ==========");

//            var feedback = GeminiRestClient.ParseJsonRobust(feedbackText);

//            // ===== POST-PROCESSING: Format polishedAnswers =====
//            feedback = PostProcessFeedback(feedback, questionType);

//            return feedback;
//        }

//        private static string GenerateSystemPrompt(string questionType, string requirements, string gradingCriteria)
//        {
//            var basePrompt = $@"⚠️ CRITICAL: YOU ARE A VIETNAMESE TEACHER GRADING TOPIK II WRITING ⚠️
//ALL FEEDBACK MUST BE IN VIETNAMESE EXCEPT KOREAN TEXT.

//QUESTION TYPE: {questionType}
//REQUIREMENTS: {requirements}
//GRADING CRITERIA: {gradingCriteria}

//";

//            var specificPrompt = questionType switch
//            {
//                "TOPIK_51" or "TOPIK_52" => GetPromptForTopik5152(),
//                "TOPIK_53" => GetPromptForTopik53(),
//                "TOPIK_54" => GetPromptForTopik54(),
//                _ => throw new ArgumentException($"Unknown question type: {questionType}")
//            };

//            var schemaPrompt = GetJsonSchema(questionType);

//            return basePrompt + specificPrompt + schemaPrompt;
//        }

//        private static JsonElement PostProcessFeedback(JsonElement feedback, string questionType)
//        {
//            if (questionType is "TOPIK_51" or "TOPIK_52")
//            {
//                if (feedback.TryGetProperty("polishedAnswers", out var polishedAnswers))
//                {
//                    var blank1 = polishedAnswers.TryGetProperty("blank1", out var b1) ? b1.GetString() : null;
//                    var blank2 = polishedAnswers.TryGetProperty("blank2", out var b2) && b2.ValueKind != JsonValueKind.Null
//                        ? b2.GetString()
//                        : null;

//                    string polishedEssay;
//                    if (!string.IsNullOrEmpty(blank2))
//                    {
//                        polishedEssay = $"ㄱ: {blank1}  ㄴ: {blank2}";
//                    }
//                    else
//                    {
//                        polishedEssay = blank1 ?? "";
//                    }

//                    var updatedJson = new Dictionary<string, object>();
//                    foreach (var prop in feedback.EnumerateObject())
//                    {
//                        if (prop.Name != "polishedAnswers")
//                        {
//                            updatedJson[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
//                        }
//                    }
//                    updatedJson["polishedEssay_kr"] = polishedEssay;

//                    var updatedJsonString = JsonSerializer.Serialize(updatedJson);
//                    return JsonSerializer.Deserialize<JsonElement>(updatedJsonString);
//                }
//            }
//            else if (questionType is "TOPIK_53" or "TOPIK_54")
//            {
//                if (feedback.TryGetProperty("polishedAnswers", out var polishedAnswers))
//                {
//                    var blank1 = polishedAnswers.TryGetProperty("blank1", out var b1) ? b1.GetString() : null;

//                    var updatedJson = new Dictionary<string, object>();
//                    foreach (var prop in feedback.EnumerateObject())
//                    {
//                        if (prop.Name != "polishedAnswers")
//                        {
//                            updatedJson[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
//                        }
//                    }
//                    updatedJson["polishedEssay_kr"] = blank1 ?? "";

//                    var updatedJsonString = JsonSerializer.Serialize(updatedJson);
//                    return JsonSerializer.Deserialize<JsonElement>(updatedJsonString);
//                }
//            }

//            return feedback;
//        }

//        private static string GetPromptForTopik5152()
//        {
//            return @"
//=== TOPIK 51/52: FILL IN THE BLANK - FULL GRADING PROMPT (VI TEACHER) ===

//ROLE:
//- You are a VIETNAMESE teacher grading TOPIK II Writing Q51/Q52.
//- ALL explanations/feedback MUST be in VIETNAMESE.
//- Korean text MUST stay in Korean (do not translate Korean sentences).
//- Output MUST be VALID JSON only (no markdown, no backticks).

//INPUTS YOU WILL RECEIVE:
//- INPUT_JSON: includes questionNo (51/52), prompt text, user submission, and image(s) (the question image).
//- The question is an image containing 1 or more blanks (㉠/㉡/ㄱ/ㄴ/...).

//========================
//STEP 0 — HARD VALIDATION (DO FIRST)
//========================

//0.1) CHECK EMPTY / WHITESPACE SUBMISSION
//- Extract user's submitted answer text(s) from INPUT_JSON.
//- If submission is null/empty/only whitespace:
//  - Add to missingInfo: ""Người học nộp bài rỗng / chỉ có khoảng trắng"".
//  - polishedAnswers must still provide a correct model answer for the blank(s) based on the image.
//  - Provide feedback/corrections accordingly.

//0.2) READ IMAGE AND COUNT BLANKS (N)
//- Analyze the image carefully.
//- Count how many blanks exist: N = 1..3 (usually 1 or 2, but handle 3 safely).
//- Identify blank markers if present: ㉠, ㉡, ㄱ, ㄴ, (1), (2), etc.
//- Record N and marker names mentally (do NOT print raw analysis; use in feedback).

//0.3) CHECK USER ANSWER COUNT vs N
//- Determine how many answers the user provided:
//  - If the user provides multiple answers, detect separators: ""ㄱ:"", ""ㄴ:"", newline, semicolon, etc.
//- If N != number_of_user_answers:
//  - Add to missingInfo: ""Số câu trả lời không khớp số chỗ trống: đề có N chỗ trống nhưng người học trả lời M"".
//  - Still grade what exists, and provide corrected full answers in polishedAnswers.
//- If N=2 or N=3: polishedAnswers must return blank1/blank2/(blank3 optional: if schema only has 2, put extra in missingInfo and merge into blank2 with format if needed).
//  - IMPORTANT: Your JSON schema here has blank1 and blank2 only.
//  - If N=3: put blank1 = answer for first, blank2 = ""ㄴ: ... ㄷ: ..."" and note in missingInfo.

//0.4) DETERMINE QUESTION TYPE (51 or 52)
//- Use questionNo:
//  - 51 => Practical/functional writing (실용문/대화/안내문), MUST be formal polite style (하십시오체/격식체).
//  - 52 => Written/expository sentence in paragraph, MUST be written declarative style (문어체/해라체).

//========================
//STEP 1 — STYLE RULES (MOST IMPORTANT)
//========================

//1.1) TOPIK 51 STYLE: 하십시오체 / 격식체 (FORMAL POLITE)
//- Acceptable sentence endings (examples):
//  - Declarative: -습니다/-ㅂ니다, -입니다
//  - Interrogative: -습니까/-ㅂ니까
//  - Request/imperative: -(으)세요 (formal polite), -(으)십시오 (more formal)
//  - Suggestion: -(으)ㅂ시다
//  - Prohibition/request not to: -지 마십시오, -지 마세요 (still formal)
//- Forbidden (flag as style error):
//  - 해요체: -아요/-어요/-해요, -네요, -죠
//  - 반말/해라체: -해, -한다, -다 (unless the blank is explicitly quoted speech requiring it)
//- If the blank is mid-sentence:
//  - Still enforce that the final sentence style of the whole sentence matches 하십시오체.
//  - The inserted segment must not force the sentence into 해요체.

//1.2) TOPIK 52 STYLE: 문어체 / 해라체 (WRITTEN DECLARATIVE)
//- Acceptable endings:
//  - -다/-는다/-ㄴ다, -이다
//  - past: -았다/-었다/-했다
//  - reporting/description: -다고 한다, -는 것으로 나타났다 (if context)
//- Forbidden:
//  - -습니다/-ㅂ니다/-세요/-십시오/-ㅂ시다 (formal polite)
//  - -아요/-어요/-해요 (polite spoken)
//- If blank is mid-sentence:
//  - Allow clause-level insertion, but ensure the overall sentence remains 문어체 and coherent.

//1.3) QUOTATION EXCEPTION (RARE)
//- If the blank is inside a direct quote (“ ”) and the quote clearly requires a different style:
//  - Follow the style demanded by the quote context.
//  - But still keep the overall answer consistent with the question's required register outside the quote.
//- If uncertain, prioritize exam-typical requirement: 51=formal polite, 52=written.

//========================
//STEP 2 — GRAMMAR & STRUCTURE CHECKLIST (FOR EACH BLANK)
//========================

//For each blank answer (user answer and your corrected answer), analyze:

//2.1) SENTENCE STRUCTURE (VIETNAMESE EXPLANATION)
//- Identify:
//  - Chủ ngữ (주어): 무엇/누가
//  - Vị ngữ (서술어): 동사/형용사/이다
//  - Tân ngữ (목적어) if any
//  - Trạng ngữ (부사어) time/place/manner
//  - Particles (조사): 이/가, 은/는, 을/를, 에/에서, (으)로, 와/과, 하고, 도, 만...
//  - Ending (종결/연결) and tense

//2.2) PARTICLE ACCURACY (조사 오류)
//Flag common issues (examples to catch):
//- 장소/기관: ""학교는"" vs ""학교에서"" (if meaning is 'at/by')
//- 대상: ""학생으로"" vs ""학생을"" depends on predicate
//- 주제: ""~을 대해"" (WRONG) -> ""~에 대해"" (RIGHT)
//- 시간: ""2020년부터 3시"" etc. incorrect
//Explain in Vietnamese why particle must be X.

//2.3) VERB/ADJ CONJUGATION & TENSE
//- Ensure tense matches context (past in announcement? general statement?).
//- Ensure honorific markers if needed (51 sometimes needs -(으)시-).

//2.4) CONNECTORS & CLAUSE FIT (if blank mid-sentence)
//- If blank is not sentence-final, check:
//  - 연결어미 compatibility: -(으)면, -(으)니까, -지만, -기 때문에, -도록, etc.
//- Do NOT blindly forbid connective endings.
//- Only flag when the blank requires a complete sentence but the user gave a dangling connector that cannot close the sentence.

//2.5) VOCABULARY & COLLOCATION (TỪ VỰNG)
//- Check:
//  - Word choice matches situation (announcement/email/dialogue).
//  - Collocations natural: 신청하다/접수하다/참석하다/문의하다/예약하다/제출하다 etc.
//  - Avoid awkward literal translations.
//- If vocabulary is unnatural but grammatically correct:
//  - Mention as ""từ vựng chưa tự nhiên"" and propose a more native alternative.

//2.6) SEMANTIC COHERENCE (NGỮ CẢNH)
//- Must match:
//  - Who is speaking (기관/개인?), audience (학생/고객?), purpose (inform/request/thank/apologize).
//  - Politeness level and tone.
//- If answer contradicts visible info in image (dates, place, instruction), flag clearly.

//========================
//STEP 3 — SCORING-LIKE FEEDBACK LOGIC (NO NUMERIC SCORE REQUIRED)
//========================

//When producing feedback:
//- Prioritize severe issues first:
//  1) Empty submission / missing answers
//  2) Wrong style (51 vs 52)
//  3) Wrong meaning / context mismatch
//  4) Grammar/particles
//  5) Vocabulary naturalness
//  6) Spelling/spacing

//========================
//OUTPUT REQUIREMENTS (JSON)
//========================

//Return JSON with EXACT structure:

//{
//  ""missingInfo"": [""... (Vietnamese) ...""],
//  ""targetCheck"": {
//    ""grammar_used"": [""... (Korean) ...""],
//    ""grammar_not_used"": [""... (Korean) ...""],
//    ""grammar_not_applicable"": [""... (Korean) ...""]
//  },
//  ""feedback"": {
//    ""overall"": [""... (Vietnamese) ...""],
//    ""coherence"": [""... (Vietnamese) ...""],
//    ""grammar"": [
//      ""... (Vietnamese - detailed) ..."",
//      ""CHỖ TRỐNG ㉠/ㄱ: ... (Vietnamese - function + grammar fit + why) ..."",
//      ""CHỖ TRỐNG ㉡/ㄴ: ... (Vietnamese - if exists) ...""
//    ],
//    ""corrections"": [
//      {""original"": ""korean"", ""fixed"": ""korean"", ""reason_vi"": ""vietnamese""}
//    ]
//  },
//  ""polishedAnswers"": {
//    ""blank1"": ""korean - correct answer for blank1"",
//    ""blank2"": ""korean or null - correct answer for blank2 (or include ㄴ: ... if needed)""
//  }
//}

//CRITICAL:
//- ALL feedback strings: Vietnamese.
//- Korean fields: must be Korean.
//- Each blank answer should be a COMPLETE usable insertion fitting the sentence.
//- Enforce correct style strictly:
//  - Q51: 하십시오체/격식체 (NOT 해요체)
//  - Q52: 문어체/해라체 (NOT -습니다)
//- If user answer count mismatch: explain in missingInfo and still provide complete polishedAnswers.
//- If user submits nonsense/irrelevant: flag and provide correction.

//";
//        }


//        private static string GetPromptForTopik53()
//        {
//            return @"
//=== ⚠️⚠️⚠️ TOPIK 53: GRAPH/CHART ANALYSIS - CRITICAL ACCURACY REQUIRED ⚠️⚠️⚠️ ===

//OVERVIEW:
//- Question Type: Describe graph/chart/survey data in paragraph form
//- Length: 200-300 KOREAN CHARACTERS (글자) - includes spaces & punctuation
//- Points: 30 points (HIGHEST single question in writing section)
//- Style: Written declarative (-다/-는다) ONLY
//- Key Principle: **ACCURACY > CREATIVITY**. This is DATA REPORTING, not opinion writing.

//=== STEP 0: IMAGE ANALYSIS - SCAN METICULOUSLY ⚠️ HIGHEST PRIORITY ===

//🔍 CRITICAL: Before writing ANYTHING, you MUST analyze the image with EXTREME PRECISION:

//**A. IDENTIFY GRAPH TYPE:**
//   1. 막대 그래프 (Bar chart) - vertical or horizontal bars
//   2. 선 그래프 (Line graph) - shows trends over time
//   3. 원 그래프/파이 차트 (Pie chart) - percentages adding to 100%
//   4. 표 (Table) - rows and columns with data
//   5. 복합 그래프 (Combined charts) - TWO OR MORE charts together

//**B. READ ALL TEXT IN IMAGE WORD BY WORD:**
//   ⚠️ CRITICAL: Extract EVERY piece of text visible:
   
//   ✅ MUST EXTRACT:
//   - 제목 (Title) - IMPORTANT: Instructions say ""글의 제목은 쓰지 마시오"" (Do NOT write title in essay)
//   - 조사 기관 (Organization) - e.g., ""한국교육연구소"", ""서울시청"", ""결혼문화연구소""
//   - 조사 대상 (Survey subjects) - e.g., ""20대 이상 성인 남녀 3,000명"", ""전국 고등학생 500명""
//   - 조사 주제/질문 (Survey topic) - e.g., ""아이를 꼭 낳아야 하는가"", ""독서량 변화""
//   - 시간 범위 (Time period) - e.g., ""2015년~2023년"", ""최근 10년간"", ""2018년 조사""
//   - 단위 (Units) - %, 명, 권, 시간, 건, 회, etc.
//   - 범례/카테고리 (Legend/Categories) - 남자/여자, 찬성/반대, 1순위/2순위/3순위
//   - X축/Y축 라벨 (Axis labels)
//   - 그래프 타입 라벨 (Chart labels) - 주요 원인, 예상 대책, etc.

//**C. EXTRACT ALL NUMERICAL DATA WITH EXTREME PRECISION:**
   
//   ⚠️ CRITICAL RULE: Write down EVERY SINGLE number, percentage, value visible
   
//   **For BAR/LINE charts:**
//   - List EVERY data point year by year or category by category
//   - Format: 2015년: 25%, 2017년: 30%, 2019년: 45%, 2021년: 55%, 2023년: 60%
//   - Note: Starting value, Ending value, Peak (highest), Lowest, Intermediate values
//   - Calculate: Change amount (e.g., 60% - 25% = 35% increase)
//   - Calculate: Change rate (e.g., 60% ÷ 25% = 2.4배, or ""약 2배"")
   
//   **For PIE charts:**
//   - List ALL segments with exact percentages
//   - Verify: Do percentages add to 100%? (CRITICAL CHECK)
//   - Example: 찬성 67% + 반대 33% = 100% ✓
//   - If 3 segments: 40% + 35% + 25% = 100% ✓
//   - Identify: Largest segment (1위), Smallest segment (꼴찌)
//   - Calculate: Difference between segments (67% - 33% = 34% 차이)
   
//   **For TABLES:**
//   - Extract ALL cells systematically: row by row, column by column
//   - Map data points to categories clearly
//   - Example:
//     ```
//     Row 1 (남자): 찬성 80%, 반대 20%
//     Row 2 (여자): 찬성 67%, 반대 33%
//     ```
   
//   **For MULTIPLE charts (VERY COMMON IN TOPIK 53):**
//   Many TOPIK 53 questions show 2-3 related charts:
   
//   - **MAIN chart** (usually top/left): Overall trend or comparison
//     Example: ""고등학생 독서량 변화: 2015년 89%, 2018년 79%, 2023년 40%""
   
//   - **SECONDARY chart** (usually bottom-left): 주요 원인 (Main causes/reasons)
//     Example: ""감소 원인: 1순위 온라인 매체 노출 증가 45%, 2순위 스마트폰 사용 증가 30%""
   
//   - **TERTIARY chart** (usually bottom-right): 예상 대책 or 전망 (Expected measures/forecast)
//     Example: ""대책: 1순위 온라인 독서 프로그램 50%, 2순위 독서 동아리 활동 35%""
   
//   ⚠️ CRITICAL: You MUST describe ALL charts, not just the first one!

//**D. CALCULATE TRENDS & COMPARISONS:**
   
//   **증가/감소 분석 (Increase/Decrease):**
//   - Calculate: From X% to Y% = |Y - X|% change
//   - Calculate: Rate = Y ÷ X = n배
//   - Example: 89% → 40% = 49% 감소, 40 ÷ 89 = 0.45 = 약 2배 감소
   
//   **비교 분석 (Comparison):**
//   - Between groups: 남자 80% vs 여자 67% = 13% 차이, 남자가 더 높다
//   - Between categories: A가 B보다 15% 높다
   
//   **순위 분석 (Ranking):**
//   - 1위 (highest), 2위 (second), 3위 (third), 꼴찌 (lowest)
//   - Must mention ranks if shown in graph
   
//   **시간 변화 (Time change):**
//   - 급증하다 (surge): >50% increase
//   - 증가하다 (increase): 20-50% increase
//   - 약간 증가하다 (slightly increase): <20% increase
//   - 급감하다 (plummet): >50% decrease
//   - 감소하다 (decrease): 20-50% decrease
//   - 약간 감소하다 (slightly decrease): <20% decrease
//   - 유지하다 (maintain): <5% change

//=== STEP 1: CHARACTER COUNT VALIDATION ===

//**REQUIRED LENGTH: 200-300 characters (글자)**

//Counting method:
//- Count: All Korean syllables (한글 글자)
//- Count: Spaces between words
//- Count: Punctuation marks (., ,, %, etc.)
//- Example: ""한국교육연구소에서는"" = 10 characters

//⚠️ PENALTIES:
//- < 200 chars → AUTOMATIC DEDUCTION: ""Bài viết quá ngắn (dưới 200 ký tự) - Giảm điểm nghiêm trọng""
//- > 300 chars → AUTOMATIC DEDUCTION: ""Bài viết quá dài (trên 300 ký tự) - Giảm điểm nghiêm trọng""

//✅ SAFE ZONE: 220-280 characters
//✅ OPTIMAL: 240-270 characters

//=== STEP 2: WRITING STYLE VALIDATION ===

//**MUST USE: Written declarative style (문어체) ONLY**

//✅ CORRECT endings:
//- -다 (present/general state): 나타나다, 차지하다, 보이다
//- -는다/-ㄴ다 (present descriptive): 보여준다, 알 수 있다
//- -았다/-었다 (past tense): 조사했다, 증가했다, 감소했다
//- -을 것이다 (future/prediction): 증가할 것이다, 개선될 것이다
//- -ㄴ/는 것으로 나타났다 (appears that): 증가하는 것으로 나타났다
//- -(으)ㄴ 반면 (whereas): 증가한 반면, 감소한 반면
//- -에 대해 (about): 독서량에 대해, 변화에 대해

//❌ FORBIDDEN endings (NEVER USE):
//- -습니다/-ㅂ니다 (formal spoken): 나타납니다 ✗, 조사했습니다 ✗
//- -아요/-어요 (polite spoken): 증가해요 ✗, 보여줘요 ✗
//- -네요 (exclamatory): 많네요 ✗, 증가하네요 ✗
//- -잖아요 (you know): 많잖아요 ✗

//⚠️ IF EVEN ONE SENTENCE uses wrong style → FLAG: ""Sai văn phong: Phải dùng văn viết (-다/-는다)""

//=== STEP 3: STRUCTURE REQUIREMENTS ===

//TOPIK 53 requires a clear **3-PART STRUCTURE**:

//**1. 서론 (Introduction) - 1-2 sentences:**
//   - State: 조사 기관 (organization)
//   - State: 조사 대상 (survey subjects)
//   - State: 조사 주제 (survey topic)
   
//   ✅ PATTERN:
//   ""[기관]에서 [대상]을 대상으로 '[주제]'에 대해 조사한 결과는 다음과 같다.""
   
//   Example:
//   ""한국교육연구소에서 전국 고등학생 500명을 대상으로 '독서량 변화'에 대해 조사한 결과는 다음과 같다.""

//**2. 본론 (Main Body) - 3-5 sentences:**
//   - Present ALL numerical data from graph
//   - Present data in LOGICAL ORDER (chronological, by ranking, by category)
//   - Mention MAIN CHART data first
//   - Then mention SECONDARY CHART (원인/reasons) if exists
//   - Then mention TERTIARY CHART (대책/measures) if exists
   
//   ✅ PATTERNS for presenting data:
//   - ""[항목]은 [년도]에 [수치], [년도]에 [수치], [년도]에 [수치]로 [기간] 동안 [변화]하였다.""
//   - ""[항목]에 대해 남자는 [수치], 여자는 [수치]로 나타났다.""
//   - ""[항목]이 가장 높았고 [수치], 다음으로 [항목] [수치], [항목] [수치] 순이었다.""
//   - ""주요 원인으로는 [원인1]과 [원인2]를 들 수 있다.""

//**3. 결론 (Conclusion) - 1 sentence:**
//   - Summarize overall trend OR
//   - Make comparison observation OR
//   - State expected outlook (if tertiary chart exists)
   
//   ✅ PATTERNS:
//   - ""이를 통해 [trend]를 알 수 있다.""
//   - ""[group A]가 [group B]에 비해 더 긍정적으로 생각하는 것을 볼 수 있다.""
//   - ""향후 [예상]할 것으로 보인다.""

//⚠️ MISSING STRUCTURE → FLAG:
//- No introduction → ""Thiếu câu mở đầu (không nêu rõ tổ chức khảo sát, đối tượng, chủ đề)""
//- No data presentation → ""Thiếu dữ liệu chính từ biểu đồ""
//- No conclusion → ""Thiếu câu kết luận""

//=== STEP 4: DATA COVERAGE VALIDATION (MOST CRITICAL) ===

//⚠️⚠️⚠️ THIS IS THE #1 REASON STUDENTS LOSE POINTS ON QUESTION 53 ⚠️⚠️⚠️

//**CHECK: Did the user mention ALL data points visible in the graph?**

//**For SINGLE chart:**
//   ✅ ALL years/categories mentioned?
//   ✅ ALL percentages/numbers stated accurately?
//   ❌ If ANY data point missing → ""Thiếu dữ liệu: Chưa nêu [specific missing data]""
   
//   Example - Line graph showing 5 years:
//   - Graph shows: 2015년 89%, 2017년 82%, 2019년 65%, 2021년 52%, 2023년 40%
//   - User wrote: ""2015년 89%, 2023년 40%로 감소했다""
//   - ERROR: Missing 2017, 2019, 2021 data → ""Thiếu 3 điểm dữ liệu (2017, 2019, 2021)""

//**For MULTIPLE charts (2-3 charts):**
//   ⚠️ CRITICAL: TOPIK 53 often has 3 charts:
//   1. Main chart (overall data)
//   2. 주요 원인 (main reasons) chart
//   3. 예상 대책 (expected measures) chart
   
//   ✅ MUST mention data from ALL 3 charts!
   
//   Example of COMPLETE coverage:
//   ```
//   한국교육연구소에서 고등학생 500명을 대상으로 독서량 변화에 대해 조사한 결과는 다음과 같다.
   
//   [MAIN CHART DATA:]
//   고등학생의 독서량은 2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다.
   
//   [SECONDARY CHART - 원인:]
//   주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다.
   
//   [TERTIARY CHART - 대책:]
//   이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다.
//   ```
   
//   ❌ INCOMPLETE - only mentioned main chart:
//   ```
//   고등학생의 독서량은 2015년 89%, 2023년 40%로 감소하였다. [MISSING 원인 & 대책 data!]
//   ```
//   → ERROR: ""Thiếu dữ liệu từ 2 biểu đồ phụ (nguyên nhân và giải pháp)""

//**For PIE charts:**
//   ✅ ALL segments mentioned?
//   ✅ Percentages add to 100%?
   
//   Example - Survey with 2 options:
//   - Graph: 찬성 67%, 반대 33%
//   - User wrote: ""찬성이 67%였다.""
//   - ERROR: Missing 반대 data → ""Thiếu dữ liệu: Chưa nêu tỷ lệ phản đối (33%)""

//**For COMPARISON data (남자 vs 여자, etc.):**
//   ✅ BOTH groups mentioned?
//   ✅ ALL subcategories for each group mentioned?
   
//   Example - Gender comparison:
//   - Graph: 남자 찬성 80%, 반대 20% / 여자 찬성 67%, 반대 33%
//   - User wrote: ""남자는 찬성 80%, 여자는 찬성 67%이다.""
//   - ERROR: Missing 반대 data → ""Thiếu dữ liệu: Chưa nêu tỷ lệ phản đối của cả hai nhóm""

//=== STEP 5: NUMBER ACCURACY VALIDATION ===

//⚠️ CRITICAL: All numbers MUST match the graph EXACTLY

//**CHECK THESE:**
//   ✅ Percentages correct? (e.g., graph shows 67%, user wrote 67% ✓)
//   ✅ Years correct? (e.g., graph shows 2015년, user wrote 2015년 ✓)
//   ✅ Rankings correct? (e.g., 1순위, 2순위, 3순위)
//   ✅ Change calculations correct?
   
//   Example calculation check:
//   - Graph: 2015년 89%, 2023년 40%
//   - User wrote: ""약 2배로 감소""
//   - Verify: 89 ÷ 40 = 2.225 ≈ 2배 ✓ CORRECT
   
//   ❌ ERRORS:
//   - Wrong percentage: Graph 67%, wrote 76% → ""Số liệu sai: Biểu đồ ghi 67% nhưng viết 76%""
//   - Wrong year: Graph 2015년, wrote 2016년 → ""Năm sai: Biểu đồ ghi 2015년 nhưng viết 2016년""
//   - Wrong calculation: 89% → 40% wrote ""감소"" (OK) but ""3배 감소"" (WRONG) → ""Tính toán sai: Giảm khoảng 2 lần, không phải 3 lần""

//=== STEP 6: GRAMMAR & EXPRESSION VALIDATION ===

//**A. PARTICLE ERRORS (조사 오류) - VERY COMMON:**

//This is the #1 grammar error category in TOPIK 53!

//Common mistakes:
//   ❌ ""한국교육연구소는"" → ✅ ""한국교육연구소에서""
//   ❌ ""학생 500명으로 대상으로"" → ✅ ""학생 500명을 대상으로""
//   ❌ ""독서량을 대해"" → ✅ ""독서량에 대해""
//   ❌ ""2015년부터 89%"" → ✅ ""2015년에 89%""
//   ❌ ""이로 통해"" → ✅ ""이를 통해""
//   ❌ ""원인이"" → ✅ ""원인으로는"" (when listing causes)

//**B. REQUIRED EXPRESSIONS for TOPIK 53:**

//You SHOULD use these patterns (they are in the official TOPIK scoring rubric):

//**조사 소개 (Survey introduction):**
//- -을/를 대상으로 (targeting): 학생들을 대상으로
//- -에 대해(서) (about): 독서량에 대해, 변화에 대해서
//- 조사한 결과는 다음과 같다 (survey results are as follows)

//**수치 제시 (Presenting numbers):**
//- -은/는 -에 -(으)로 나타났다: 찬성은 67%로 나타났다
//- -은/는 -이/가 -를 차지했다: 1순위는 온라인 매체가 45%를 차지했다
//- -년간 약 -배로 증가/감소하였다: 8년간 약 2배로 감소하였다

//**비교 (Comparison):**
//- -에 비해 (compared to): 여자에 비해, 2015년에 비해
//- -(으)ㄴ 반면 (whereas): 증가한 반면, 남자는 높은 반면
//- -보다 (more than): 남자보다, 이전보다

//**원인/대책 (Causes/Measures):**
//- -을/를 들 수 있다 (can cite): 주요 원인으로 A와 B를 들 수 있다
//- -기 때문이다 (because): 증가했기 때문이다
//- -에 따른 (following from): 이에 따른 대책으로는

//**C. VERB TENSE CONSISTENCY:**
//   ✅ Past tense for completed surveys: 조사했다, 나타났다, 증가했다
//   ✅ Present tense for current state: 차지한다, 볼 수 있다
//   ❌ Future tense RARELY used in 53: 증가할 것이다 (only if graph shows forecast)

//**D. SENTENCE CONNECTION:**
//   Use appropriate connectors:
//   - 그리고, 또한, 게다가 (and, moreover)
//   - 반면, 한편 (on the other hand)
//   - 따라서, 그래서 (therefore)
   
//   ⚠️ Don't overuse same connector → ""Lặp từ nối '그리고' quá nhiều""

//=== STEP 7: COMMON ERRORS TO FLAG ===

//Based on research, these are the MOST COMMON errors in TOPIK 53:

//**ERROR #1: 수치 누락 (Missing numbers) - 40% of errors**
//   → ""Thiếu dữ liệu: Biểu đồ có [X] nhưng bài viết chỉ nêu [Y]""

//**ERROR #2: 조사 오류 (Particle mistakes) - 25% of errors**
//   → Examples listed in Step 6A above

//**ERROR #3: 2개 이상 그래프 중 일부만 서술 (Only describing 1 of multiple charts) - 15%**
//   → ""Thiếu dữ liệu: Đề có 3 biểu đồ nhưng chỉ viết về 1 biểu đồ chính""

//**ERROR #4: 수치 정확성 오류 (Wrong numbers) - 10%**
//   → ""Số liệu sai: Biểu đồ ghi X% nhưng viết Y%""

//**ERROR #5: 글자 수 부족/초과 (Wrong character count) - 5%**
//   → ""Quá ngắn: [count] ký tự (cần 200-300)"" or ""Quá dài: [count] ký tự (cần 200-300)""

//**ERROR #6: 문어체 미사용 (Not using written style) - 3%**
//   → ""Sai văn phong: Dùng -습니다/-아요 thay vì -다/-는다""

//**ERROR #7: 구조 누락 (Missing structure) - 2%**
//   → ""Thiếu câu mở đầu"" or ""Thiếu kết luận""

//=== FEEDBACK STRUCTURE FOR TOPIK 53 ===

//{{
//  ""missingInfo"": [
//    ""vietnamese - LIST ALL ISSUES, prioritize by severity:"",
//    ""1. Thiếu dữ liệu: [specific missing data points]"",
//    ""2. Số liệu sai: [specific wrong numbers]"",
//    ""3. Thiếu biểu đồ: [which charts not mentioned]"",
//    ""4. Độ dài: [if < 200 or > 300 chars]"",
//    ""5. Văn phong: [if wrong style used]"",
//    ""6. Cấu trúc: [if missing intro/conclusion]"",
//    ""7. Ngữ pháp: [major grammar issues]""
//  ],
//  ""targetCheck"": {{
//    ""grammar_used"": [
//      ""korean - grammar patterns user ACTUALLY used correctly"",
//      ""Examples: -을/를 대상으로, -에 대해, -(으)로 나타났다""
//    ],
//    ""grammar_not_used"": [
//      ""korean - appropriate grammar for this chart type but NOT used"",
//      ""Examples: -(으)ㄴ 반면, -을/를 차지하다, -기 때문이다""
//    ],
//    ""grammar_not_applicable"": [
//      ""korean - grammar user selected but not suitable for this data type"",
//      ""Example: User selected -(으)면서 but it's for simultaneous actions, not data reporting""
//    ]
//  }},
//  ""feedback"": {{
//    ""overall"": [
//      ""vietnamese - COMPREHENSIVE assessment:"",
//      ""Độ dài: [X] ký tự ([đạt/quá ngắn/quá dài])"",
//      ""Cấu trúc: [có đủ mở bài - thân bài - kết luận không]"",
//      ""Dữ liệu: [đã nêu đủ/thiếu X điểm dữ liệu]"",
//      ""Chính xác: [số liệu đúng/sai Y chỗ]"",
//      ""Văn phong: [đúng văn viết/sai văn nói]""
//    ],
//    ""coherence"": [
//      ""vietnamese - DATA LOGIC & ORGANIZATION:"",
//      ""Thứ tự trình bày: [hợp lý/chưa hợp lý - lý do]"",
//      ""Liên kết dữ liệu: [mượt mà/thiếu từ nối]"",
//      ""Phân tích xu hướng: [có/không có nhận xét về xu hướng]"",
//      ""Đầy đủ biểu đồ: [đã trình bày [n]/[m] biểu đồ]""
//    ],
//    ""grammar"": [
//      ""vietnamese - DETAILED GRAMMAR ANALYSIS:"",
//      ""📊 KIỂM TRA TRỢ TỪ (Particles): [list specific errors with corrections]"",
//      ""📊 KIỂM TRA VĂN PHONG (Style): [all sentences use -다/-는다? flag any -습니다/-아요]"",
//      ""📊 KIỂM TRA CỤM TỪ CỐ ĐỊNH (Fixed expressions): [check -을/를 대상으로, -에 대해, etc.]"",
//      ""📊 KIỂM TRA THÌ (Tense): [past tense for completed survey? present for current state?]"",
//      ""📊 KIỂM TRA CHÍNH TẢ (Spelling): [any spelling errors]"",
//      ""📊 KIỂM TRA NGỮ PHÁP ĐÃ CHỌN (Selected grammar): [did user use them correctly & appropriately?]""
//    ],
//    ""corrections"": [
//      {{
//        ""original"": ""korean - WRONG sentence or phrase from user's essay"",
//        ""fixed"": ""korean - CORRECTED version"",
//        ""reason_vi"": ""vietnamese - DETAILED explanation: what's wrong, why wrong, how to fix, which rule applies""
//      }},
//      ""NOTE: Prioritize corrections for:"",
//      ""1. Missing data (add the missing numbers)"",
//      ""2. Wrong numbers (fix to match graph)"",
//      ""3. Particle errors (fix 조사)"",
//      ""4. Style errors (change -습니다 to -다)"",
//      ""5. Structure issues (add missing intro/conclusion)""
//    ]
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""korean - COMPLETE REVISED PARAGRAPH with:"",
//    ""  - Proper introduction (기관, 대상, 주제)"",
//    ""  - ALL data from ALL charts"",
//    ""  - Accurate numbers"",
//    ""  - Correct particles"",
//    ""  - Written style (-다/-는다)"",
//    ""  - Logical structure"",
//    ""  - 200-300 characters total"",
//    ""blank2"": null
//  }}
//}}

//=== EXAMPLES FOR TOPIK 53 ===

//**EXAMPLE 1: COMPLETE & CORRECT ANSWER**

//Image description:
//- Main chart: 고등학생 독서량 (2015: 89%, 2018: 79%, 2023: 40%)
//- Secondary chart: 감소 원인 (1순위 온라인 매체 45%, 2순위 스마트폰 30%)
//- Tertiary chart: 예상 대책 (1순위 온라인 독서 프로그램 50%, 2순위 독서 동아리 35%)

//User submission (270 chars):
//""한국교육연구소에서 전국 고등학생 500명을 대상으로 독서량 변화에 대해 조사한 결과는 다음과 같다. 고등학생의 독서량은 2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다. 주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다. 이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다.""

//{{
//  ""missingInfo"": [],
//  ""targetCheck"": {{
//    ""grammar_used"": [
//      ""-을/를 대상으로 (used correctly: 500명을 대상으로)"",
//      ""-에 대해 (used correctly: 변화에 대해)"",
//      ""-을/를 들 수 있다 (used correctly: 원인으로는...를 들 수 있다)"",
//      ""-(으)로 나타났다 pattern implied""
//    ],
//    ""grammar_not_used"": [
//      ""-(으)ㄴ 반면 (could compare: 증가한 반면 독서량은 감소했다)"",
//      ""-을/를 차지하다 (could use for percentages)""
//    ],
//    ""grammar_not_applicable"": []
//  }},
//  ""feedback"": {{
//    ""overall"": [
//      ""Bài viết xuất sắc! Độ dài: 270 ký tự (đạt chuẩn 200-300). Cấu trúc đầy đủ: có mở bài (giới thiệu khảo sát), thân bài (trình bày đủ 3 biểu đồ), và kết luận. Dữ liệu: Đã nêu đầy đủ TẤT CẢ số liệu từ cả 3 biểu đồ. Chính xác: Tất cả số liệu đều chính xác 100%. Văn phong: Hoàn toàn đúng văn viết (-다/-는다).""
//    ],
//    ""coherence"": [
//      ""Thứ tự trình bày rất hợp lý: Mở đầu → Dữ liệu chính → Nguyên nhân → Giải pháp. Liên kết giữa các ý mượt mà, sử dụng từ nối phù hợp. Đã phân tích xu hướng (급감 - giảm mạnh) và tính toán chính xác (약 2배). Đầy đủ: Đã trình bày cả 3/3 biểu đồ.""
//    ],
//    ""grammar"": [
//      ""✅ TRỢ TỪ: Hoàn hảo - '을 대상으로', '에 대해', '으로는', '로' tất cả đều đúng."",
//      ""✅ VĂN PHONG: Đúng 100% - tất cả câu kết thúc bằng -다/-는다/-였다."",
//      ""✅ CỤM TỪ CỐ ĐỊNH: Xuất sắc - đã dùng đúng các cụm '대상으로', '에 대해', '들 수 있다'."",
//      ""✅ THÌ: Chính xác - dùng quá khứ cho khảo sát (조사한), xu hướng (급감하였다)."",
//      ""✅ CHÍNH TẢ: Không có lỗi."",
//      ""✅ NGỮ PHÁP ĐÃ CHỌN: Đã sử dụng hiệu quả và chính xác.""
//    ],
//    ""corrections"": []
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""한국교육연구소에서 전국 고등학생 500명을 대상으로 독서량 변화에 대해 조사한 결과는 다음과 같다. 고등학생의 독서량은 2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다. 주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다. 이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다."",
//    ""blank2"": null
//  }}
//}}

//**EXAMPLE 2: MISSING DATA FROM SECONDARY CHARTS**

//Image: Same 3 charts as Example 1

//User submission (150 chars):
//""한국교육연구소에서 고등학생을 대상으로 독서량을 조사했다. 2015년 89%에서 2023년 40%로 감소했다.""

//{{
//  ""missingInfo"": [
//    ""Thiếu dữ liệu NGHIÊM TRỌNG: Đề có 3 biểu đồ nhưng chỉ viết về biểu đồ chính"",
//    ""Thiếu hoàn toàn dữ liệu từ biểu đồ nguyên nhân (온라인 매체 45%, 스마트폰 30%)"",
//    ""Thiếu hoàn toàn dữ liệu từ biểu đồ giải pháp (온라인 독서 프로그램 50%, 독서 동아리 35%)"",
//    ""Thiếu điểm dữ liệu năm 2018 (79%) từ biểu đồ chính"",
//    ""Bài viết quá ngắn: 150 ký tự (cần 200-300 ký tự) - giảm điểm nghiêm trọng"",
//    ""Thiếu câu kết luận"",
//    ""Lỗi trợ từ: '독서량을' nên là '독서량에 대해'""
//  ],
//  ""targetCheck"": {{
//    ""grammar_used"": [
//      ""-을/를 대상으로 (dùng đúng nhưng chưa đầy đủ: thiếu số lượng 500명)""
//    ],
//    ""grammar_not_used"": [
//      ""-에 대해 (nên dùng: 독서량에 대해)"",
//      ""-을/를 들 수 있다 (cần thiết để liệt kê nguyên nhân)"",
//      ""-(으)로 나타났다 (nên dùng khi trình bày kết quả)""
//    ],
//    ""grammar_not_applicable"": []
//  }},
//  ""feedback"": {{
//    ""overall"": [
//      ""Bài viết CÓ VẤN ĐỀ NGHIÊM TRỌNG. Độ dài: CHỈ 150 ký tự (thiếu 50 ký tự so với yêu cầu tối thiểu 200). Cấu trúc: Thiếu kết luận. Dữ liệu: CHỈ trình bày 1/3 biểu đồ - thiếu hoàn toàn 2 biểu đồ phụ về nguyên nhân và giải pháp. Ngay cả biểu đồ chính cũng thiếu năm 2018. Văn phong: Đúng (-다/-했다) nhưng quá ngắn gọn.""
//    ],
//    ""coherence"": [
//      ""Thứ tự: Có logic cơ bản (mở đầu → dữ liệu) nhưng THIẾU HOÀN TOÀN phần nguyên nhân và giải pháp. Bài viết chỉ dừng lại ở việc nêu xu hướng giảm mà không giải thích tại sao và làm thế nào khắc phục - đây là 2/3 nội dung bắt buộc của đề bài!""
//    ],
//    ""grammar"": [
//      ""❌ TRỢ TỪ: SAI - '독서량을 조사했다' nên là '독서량에 대해 조사했다' (phải dùng -에 대해 khi nói về chủ đề khảo sát)"",
//      ""✅ VĂN PHONG: Đúng - dùng -했다/-했다"",
//      ""⚠️ CỤM TỪ: Thiếu số lượng đối tượng '500명을 대상으로' chỉ viết '고등학생을 대상으로'"",
//      ""⚠️ THIẾU biểu thức '들 수 있다' để liệt kê nguyên nhân""
//    ],
//    ""corrections"": [
//      {{
//        ""original"": ""독서량을 조사했다"",
//        ""fixed"": ""독서량에 대해 조사한 결과는 다음과 같다"",
//        ""reason_vi"": ""Sửa trợ từ: Phải dùng '-에 대해' (about) khi nói về chủ đề khảo sát, không dùng trực tiếp tân ngữ '-을/를'. Thêm 'an 결과는 다음과 같다' để mở đầu chuẩn.""
//      }},
//      {{
//        ""original"": ""[BÀI VIẾT THIẾU HOÀN TOÀN] Không có câu nào về nguyên nhân và giải pháp"",
//        ""fixed"": ""[CẦN THÊM] 주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다. 이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다."",
//        ""reason_vi"": ""BẮT BUỘC phải thêm dữ liệu từ 2 biểu đồ còn lại. Biểu đồ nguyên nhân (주요 원인) và biểu đồ giải pháp (예상 대책) là nội dung BẮT BUỘC của đề bài. Thiếu 2 phần này sẽ mất rất nhiều điểm ở tiêu chí 'nội dung đầy đủ' (40% tổng điểm câu 53).""
//      }},
//      {{
//        ""original"": ""2015년 89%에서 2023년 40%로 감소했다"",
//        ""fixed"": ""2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다"",
//        ""reason_vi"": ""Thêm điểm dữ liệu năm 2018 (79%) - không được bỏ sót bất kỳ năm nào có trong biểu đồ. Thêm tính toán cụ thể '약 2배로' (khoảng gấp 2 lần) và dùng từ chính xác hơn '급감' (giảm mạnh) thay vì '감소' (giảm).""
//      }}
//    ]
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""한국교육연구소에서 전국 고등학생 500명을 대상으로 독서량 변화에 대해 조사한 결과는 다음과 같다. 고등학생의 독서량은 2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다. 주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다. 이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다."",
//    ""blank2"": null
//  }}
//}}

//**EXAMPLE 3: WRONG NUMBERS**

//Image: Survey about having children
//- 조사: 결혼문화연구소, 20대 이상 성인 남녀 3,000명, ""아이를 꼭 낳아야 하는가""
//- Results: 남자 찬성 80%, 반대 20% / 여자 찬성 67%, 반대 33%

//User submission (230 chars):
//""결혼문화연구소에서는 20대 이상 성인 남녀 3,000명을 대상을 '아이를 꼭 낳아야 하는가'에 대해 조사하였다. 남자는 '그렇다'가 76%, '아니다'가 24%이고, 여자는 '그렇다'가 67%, '아니다'가 33%였다.""

//{{
//  ""missingInfo"": [
//    ""Số liệu SAI NGHIÊM TRỌNG: Nam giới 찬성 - Biểu đồ ghi 80% nhưng viết 76%"",
//    ""Số liệu SAI: Nam giới 반대 - Biểu đồ ghi 20% nhưng viết 24%"",
//    ""Lỗi trợ từ: '대상을' nên là '대상으로'"",
//    ""Thiếu câu kết luận: Nên có nhận xét so sánh giữa nam và nữ""
//  ],
//  ""targetCheck"": {{
//    ""grammar_used"": [
//      ""-을/를 대상으로 (dùng sai trợ từ: 대상을 → nên là 대상으로)"",
//      ""-에 대해 (đúng)""
//    ],
//    ""grammar_not_used"": [
//      ""-에 비해 (nên dùng để so sánh: 여자에 비해)"",
//      ""-는 것을 볼 수 있다 (nên dùng trong kết luận)""
//    ],
//    ""grammar_not_applicable"": []
//  }},
//  ""feedback"": {{
//    ""overall"": [
//      ""Bài viết CÓ LỖI NGHIÊM TRỌNG về số liệu. Độ dài: 230 ký tự (đạt chuẩn). Cấu trúc: Có mở bài và thân bài, NHƯNG THIẾU kết luận. Dữ liệu: Đã nêu đủ 4 số liệu NHƯNG 2 SỐ BỊ SAI. Chính xác: 2/4 số liệu SAI (nam 80%→76%, 20%→24%). Văn phong: Đúng văn viết.""
//    ],
//    ""coherence"": [
//      ""Thứ tự: Hợp lý - giới thiệu rồi trình bày dữ liệu. NHƯNG thiếu câu kết luận để so sánh/nhận xét (ví dụ: 'Nam giới có quan điểm tích cực hơn nữ giới'). Liên kết: OK nhưng hơi đơn điệu.""
//    ],
//    ""grammar"": [
//      ""❌ TRỢ TỪ: SAI - '대상을' phải sửa thành '대상으로'. Đây là cụm cố định '-을/를 대상으로' (targeting), không thể dùng tân ngữ '-을/를' thông thường."",
//      ""✅ VĂN PHONG: Đúng - dùng -다/-였다"",
//      ""✅ CỤM TỪ: '에 대해' dùng đúng"",
//      ""⚠️ THIẾU: Câu kết luận với '볼 수 있다' hoặc '나타났다'""
//    ],
//    ""corrections"": [
//      {{
//        ""original"": ""3,000명을 대상을"",
//        ""fixed"": ""3,000명을 대상으로"",
//        ""reason_vi"": ""Sửa trợ từ: Cụm cố định là '-을/를 대상으로' (nhắm đến đối tượng). Không thể dùng '대상을' vì 대상 ở đây không phải tân ngữ trực tiếp mà là thành phần bổ ngữ chỉ phạm vi.""
//      }},
//      {{
//        ""original"": ""남자는 '그렇다'가 76%, '아니다'가 24%"",
//        ""fixed"": ""남자는 '그렇다'가 80%, '아니다'가 20%"",
//        ""reason_vi"": ""SỬA SỐ LIỆU SAI: Theo biểu đồ, nam giới trả lời 찬성 (그렇다) là 80%, không phải 76%. Phản đối (아니다) là 20%, không phải 24%. Đây là lỗi NGHIÊM TRỌNG vì câu 53 yêu cầu độ chính xác tuyệt đối về số liệu - viết sai số sẽ mất nhiều điểm ở tiêu chí 'độ chính xác số liệu' (30% tổng điểm).""
//      }},
//      {{
//        ""original"": ""[THIẾU] Không có câu kết luận"",
//        ""fixed"": ""이를 통해 남자가 여자에 비해 아이를 낳는 것에 대해 더 긍정적으로 생각하는 것을 볼 수 있다."",
//        ""reason_vi"": ""CẦN THÊM câu kết luận để so sánh/nhận xét. Bài viết không nên dừng lại ở việc liệt kê số liệu mà phải có câu tổng kết (예: Nam giới suy nghĩ tích cực hơn nữ giới về việc sinh con). Dùng cấu trúc '-에 비해' (so với) và '-는 것을 볼 수 있다' (có thể thấy rằng).""
//      }}
//    ]
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""결혼문화연구소에서 20대 이상 성인 남녀 3,000명을 대상으로 '아이를 꼭 낳아야 하는가'에 대해 조사한 결과는 다음과 같다. 남자는 '그렇다'가 80%, '아니다'가 20%로 나타났고, 여자는 '그렇다'가 67%, '아니다'가 33%로 나타났다. 이를 통해 남자가 여자에 비해 아이를 낳는 것에 대해 더 긍정적으로 생각하는 것을 볼 수 있다."",
//    ""blank2"": null
//  }}
//}}

//**EXAMPLE 4: WRONG STYLE (Using -습니다 instead of -다)**

//User submission:
//""한국교육연구소에서 학생들을 대상으로 독서량을 조사했습니다. 2015년에는 89%였지만 2023년에는 40%로 감소했습니다.""

//{{
//  ""missingInfo"": [
//    ""SAI VĂN PHONG NGHIÊM TRỌNG: Dùng văn nói -습니다/-었습니다 thay vì văn viết -다/-였다"",
//    ""Thiếu dữ liệu: Chỉ có 2 năm, thiếu năm 2018 (79%)"",
//    ""Thiếu cấu trúc: Không có câu mở đầu chuẩn 'an 결과는 다음과 같다'"",
//    ""Thiếu các biểu đồ phụ (nếu có): Không nêu nguyên nhân và giải pháp"",
//    ""Lỗi trợ từ: '독서량을' nên là '독서량에 대해'"",
//    ""Bài quá ngắn: Khoảng 100 ký tự (cần 200-300)""
//  ],
//  ""targetCheck"": {{
//    ""grammar_used"": [],
//    ""grammar_not_used"": [
//      ""-을/를 대상으로"",
//      ""-에 대해"",
//      ""-(으)로 나타났다"",
//      ""-을/를 들 수 있다""
//    ],
//    ""grammar_not_applicable"": []
//  }},
//  ""feedback"": {{
//    ""overall"": [
//      ""Bài viết SAI VĂN PHONG HOÀN TOÀN - đây là lỗi CỰC KỲ NGHIÊM TRỌNG trong câu 53. Tất cả câu đều dùng -습니다/-었습니다 (văn nói) trong khi câu 53 BẮT BUỘC phải dùng -다/-는다/-였다 (văn viết). Ngoài ra còn thiếu rất nhiều dữ liệu và quá ngắn.""
//    ],
//    ""coherence"": [
//      ""Không thể đánh giá tính mạch lạc khi văn phong đã sai hoàn toàn. Bài viết này sẽ bị GIẢM ĐIỂM RẤT NẶNG ở tiêu chí 'ngữ pháp/từ vựng' (30% tổng điểm câu 53).""
//    ],
//    ""grammar"": [
//      ""❌❌❌ VĂN PHONG: SAI HOÀN TOÀN - TOPIK 53 BẮT BUỘC dùng văn viết (-다/-는다/-였다) KHÔNG được dùng -습니다/-아요/-어요"",
//      ""Cần sửa: '조사했습니다' → '조사한 결과는 다음과 같다'"",
//      ""Cần sửa: '였지만' → '였으나' hoặc '인 반면'"",
//      ""Cần sửa: '감소했습니다' → '감소하였다' hoặc '급감하였다'"",
//      ""❌ TRỢ TỪ: '독서량을' → '독서량에 대해'""
//    ],
//    ""corrections"": [
//      {{
//        ""original"": ""조사했습니다"",
//        ""fixed"": ""조사한 결과는 다음과 같다"",
//        ""reason_vi"": ""SỬA VĂN PHONG: Phải đổi '-었습니다' (văn nói lịch sự) thành '-다' (văn viết). Câu 53 TUYỆT ĐỐI KHÔNG được dùng hình thức lịch sự -습니다. Thêm 'an 결과는 다음과 같다' là cụm mở đầu chuẩn cho bài mô tả biểu đồ.""
//      }},
//      {{
//        ""original"": ""감소했습니다"",
//        ""fixed"": ""급감하였다"",
//        ""reason_vi"": ""SỬA VĂN PHONG: '-었습니다' → '-였다'. Dùng '급감' (giảm mạnh) thay vì '감소' (giảm) để chính xác hơn khi giảm từ 89% xuống 40% (giảm hơn 50%).""
//      }},
//      {{
//        ""original"": ""독서량을 조사했습니다"",
//        ""fixed"": ""독서량 변화에 대해 조사한 결과는 다음과 같다"",
//        ""reason_vi"": ""Sửa cấu trúc: Phải dùng '-에 대해' (about) cho chủ đề khảo sát. Thêm '변화' (thay đổi) nếu biểu đồ về xu hướng theo thời gian. Đổi '-었습니다' thành '-다'.""
//      }}
//    ]
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""한국교육연구소에서 전국 고등학생 500명을 대상으로 독서량 변화에 대해 조사한 결과는 다음과 같다. 고등학생의 독서량은 2015년 89%, 2018년 79%, 2023년 40%로 8년간 약 2배로 급감하였다. 주요 감소 원인으로는 온라인 매체 노출 증가 45%와 스마트폰 사용 증가 30%를 들 수 있다. 이에 따른 예상 대책으로는 온라인 독서 프로그램 참여 50%와 독서 동아리 활동 35%가 제시되었다."",
//    ""blank2"": null
//  }}
//}}

//=== CRITICAL REMINDERS FOR TOPIK 53 ===

//1. ⚠️ SCAN IMAGE METICULOUSLY - extract EVERY number, percentage, year, category
//2. ⚠️ COUNT CHARTS - if 2 or 3 charts, you MUST describe ALL of them
//3. ⚠️ VERIFY NUMBERS - every single number must match graph EXACTLY
//4. ⚠️ CHECK CHARACTER COUNT - must be 200-300 chars (220-280 is safe zone)
//5. ⚠️ VERIFY WRITING STYLE - ONLY -다/-는다/-였다, NEVER -습니다/-아요/-어요
//6. ⚠️ CHECK PARTICLES - especially -을/를 대상으로, -에 대해
//7. ⚠️ USE REQUIRED PATTERNS - 조사한 결과는 다음과 같다, -을/를 들 수 있다
//8. ⚠️ INCLUDE STRUCTURE - intro (기관+대상+주제) + body (all data) + conclusion
//9. ⚠️ ACCURACY > CREATIVITY - this is objective data reporting, not opinion
//10. ⚠️ PRIORITIZE COMPLETENESS - better to include all data at 290 chars than miss data at 250 chars
//";
//        }

//        private static string GetPromptForTopik54()
//        {
//            return @"
//=== ⚠️⚠️⚠️ TOPIK 54: ARGUMENTATIVE/EXPLANATORY ESSAY - 50 POINTS (HIGHEST!) ⚠️⚠️⚠️ ===

//OVERVIEW:
//- Length: 600-700 KOREAN CHARACTERS (글자) including spaces & punctuation
//- Points: 50 points - 50% of entire writing section!
//- Style: Written declarative (-다/-는다/-았다) ONLY - NO -습니다/-아요 allowed
//- Structure: Introduction → Body (2-3 paragraphs) → Conclusion
//- Sub-questions: ALWAYS 2-3 questions - MUST answer ALL
//- Key: LOGICAL COHERENCE + ADVANCED VOCABULARY + COMPLETE TASK FULFILLMENT

//CRITICAL WARNINGS:
//- Missing ANY sub-question = lose 15-20 points immediately
//- Wrong style (-습니다/-아요) throughout = lose 15-20 points
//- Off-topic = could fail entirely
//- Length <550 or >750 = automatic deduction before grading

//=== STEP 0: INSTANT FAIL CONDITIONS (Check First!) ===

//❌ FAIL #1: Empty/nearly empty submission → ""Bài viết trống - không thể chấm""
//❌ FAIL #2: Spam/gibberish (ㅋㅋㅋ, asdf, 123) → ""Spam/vô nghĩa""
//❌ FAIL #3: Significant non-Korean text (emojis, Latin) → ""Chỉ được viết Hangul""
//   Exception: If prompt contains foreign terms (AI, COVID-19), those OK
//❌ FAIL #4: Completely off-topic → ""Hoàn toàn lạc đề""
//❌ FAIL #5: Copied question word-for-word → ""Vi phạm: Copy nguyên văn đề bài""
//   Note: Copying 5+ consecutive words from question = plagiarism

//IF ANY FAIL → Return error and STOP grading immediately.

//=== STEP 1: ANALYZE SUB-QUESTIONS (CRITICAL!) ===

//⚠️ TOPIK 54 ALWAYS has 2-3 sub-questions that MUST ALL be answered!

//**Identify essay type:**

//TYPE 1: Problem-Solving (40%)
//- Sub-Q1: What problems? (문제점은?)
//- Sub-Q2: What causes? (원인은?)
//- Sub-Q3: What solutions? (해결 방안은?)
//Examples: 환경 오염, 스마트폰 중독, 청소년 문제

//TYPE 2: Argumentative (35%)
//- Sub-Q1: Your opinion? (당신의 생각은?)
//- Sub-Q2: Why? Evidence? (왜 그렇게 생각하는가?)
//- Sub-Q3: What should we do? (우리가 해야 할 일은?)
//Examples: 조기 외국어 교육, 기술과 행복, 온라인 수업

//TYPE 3: Explanatory (25%)
//- Sub-Q1: Define concept ([개념]이란?)
//- Sub-Q2: Why important? (왜 중요한가?)
//- Sub-Q3: How to practice? (어떻게 실천?)
//Examples: 창의력, 평생 교육, 리더십

//**Essay structure planning:**
//```
//Introduction (100-150 chars): Topic intro + thesis + preview
//Body Paragraph 1 (200-250 chars): Answer Sub-Q2 with 2-3 points + examples
//Body Paragraph 2 (200-250 chars): Answer Sub-Q3 with 2-3 points + examples
//Conclusion (100-150 chars): Summary + final thought
//TOTAL: 600-700 characters
//```

//⚠️ CHECK: Does user answer ALL sub-questions?
//- Missing Sub-Q1? → ""Thiếu câu trả lời cho câu hỏi 1: [quote question]""
//- Missing Sub-Q2? → ""Thiếu câu trả lời cho câu hỏi 2: [quote question]""
//- Missing Sub-Q3? → ""Thiếu câu trả lời cho câu hỏi 3: [quote question]""

//Missing even ONE sub-question = 15-20 point loss!

//=== STEP 2: CHARACTER COUNT VALIDATION ===

//Count method: Korean syllables + spaces + punctuation
//Example: ""환경 오염은 심각한 문제이다."" = 15 chars (14 syllables + 1 period)

//⚠️ LENGTH PENALTIES:
//- **<550 chars** → ""QUÁ NGẮN NGHIÊM TRỌNG: [X] ký tự (cần 600+)"" → Lose 10-15 pts
//- **550-599** → ""Hơi ngắn: [X] ký tự"" → Lose 2-5 pts
//- **701-750** → ""Hơi dài: [X] ký tự"" → Lose 2-5 pts
//- **>750** → ""QUÁ DÀI: [X] ký tự"" → Lose 5-10 pts

//✅ SAFE ZONES: 600-620 (minimum), 640-670 (optimal), 680-700 (maximum)

//=== STEP 3: WRITING STYLE VALIDATION (CRITICAL!) ===

//**MUST USE (Written style):**
//✅ -다 (이다, 크다, 작다)
//✅ -는다/-ㄴ다 (보여준다, 알려준다)
//✅ -았다/-었다 (증가했다, 감소했다)
//✅ -을 것이다 (개선될 것이다)
//✅ -(으)며, -(으)면서, -기 때문이다, -(으)므로, -(으)ㄴ 반면 (connectives OK)

//**NEVER USE (Spoken style):**
//❌ -습니다/-ㅂ니다 (formal polite)
//❌ -습니까?/-ㅂ니까? (formal questions)
//❌ -아요/-어요 (polite informal)
//❌ -네요, -잖아요 (exclamatory)
//❌ -어/-아, -지, -거든 (casual)

//⚠️ STYLE VIOLATIONS:
//- 1-2 sentences wrong → ""Lỗi nhỏ: [X] câu dùng -습니다"" → Lose 3-5 pts
//- 3-5 sentences wrong → ""NGHIÊM TRỌNG: [X] câu văn nói"" → Lose 10-15 pts
//- 6+ or ALL wrong → ""SAI HOÀN TOÀN văn phong"" → Lose 15-20+ pts

//=== STEP 4: STRUCTURE & COHERENCE ===

//**Paragraph structure:**
//✅ MUST have: Introduction (separate) + Body paragraphs (2-3) + Conclusion (separate)
//❌ Missing intro → ""Thiếu đoạn mở bài""
//❌ Missing body → ""Thiếu phần thân bài""
//❌ Missing conclusion → ""Thiếu kết luận""
//❌ All one paragraph → ""Không có cấu trúc đoạn văn""

//**Transitions between paragraphs:**
//- Intro → Body: 구체적으로 살펴보면, 먼저, 우선
//- Body 1 → Body 2: 또한, 다음으로, 이와 더불어, 이러한 문제를 해결하기 위해서는
//- Body 2 → Conclusion: 따라서, 그러므로, 이상에서 살펴본 바와 같이, 결론적으로

//**Within paragraphs:**
//- Sequential: 첫째/둘째/셋째, 먼저/다음으로/마지막으로
//- Adding: 또한, 게다가, 뿐만 아니라, 아울러
//- Examples: 예를 들어, 구체적으로 말하면, 예컨대
//- Contrasting: 그러나, 하지만, 반면, 이와 달리
//- Cause-effect: -기 때문이다, -(으)므로, 그 결과, 이에 따라

//⚠️ COHERENCE ERRORS:
//- No transitions → ""Thiếu liên kết giữa đoạn - chuyển đột ngột""
//- Overuse connector → ""Lạm dụng '[word]' - dùng [X] lần""
//- Illogical sequence → ""Trình tự logic kém""
//- Ideas don't connect → ""Các ý không liên kết""

//=== STEP 5: VOCABULARY LEVEL (Level 5-6 Required!) ===

//**BASIC → ADVANCED replacements:**

//Adjectives:
//❌ 좋다 → ✅ 유익하다, 긍정적이다, 바람직하다, 효과적이다
//❌ 나쁘다 → ✅ 부정적이다, 해롭다, 문제가 있다, 악영향을 미치다
//❌ 중요하다 → ✅ 필수적이다, 핵심적이다, 중대하다, 결정적이다
//❌ 많다 → ✅ 다양하다, 풍부하다, 증가하다, 다수이다
//❌ 어렵다 → ✅ 복잡하다, 난해하다, 까다롭다

//Verbs:
//❌ 하다 → ✅ 수행하다, 실시하다, 진행하다, 추진하다
//❌ 생각하다 → ✅ 인식하다, 파악하다, 고려하다, 판단하다
//❌ 알다 → ✅ 인지하다, 파악하다, 이해하다
//❌ 주다 → ✅ 제공하다, 부여하다, 제시하다
//❌ 만들다 → ✅ 조성하다, 구축하다, 형성하다

//Nouns:
//❌ 사람들 → ✅ 대중, 국민, 시민, 구성원
//❌ 것 → ✅ 사항, 측면, 요소, 부분
//❌ 문제 → ✅ 과제, 쟁점, 사안, 현안

//**COLLOCATIONS (Fixed expressions - VERY important!):**
//✅ 관계를 형성하다/맺다/유지하다 (NOT 만들다 ❌)
//✅ 노력을 기울이다/하다/다하다 (NOT 주다 ❌)
//✅ 영향을 미치다/끼치다 (NOT 하다/주다 ❌)
//✅ 중요성을 인식하다/강조하다 (NOT 알다 ❌)
//✅ 문제를 해결하다/직면하다 (NOT 풀다 ❌)
//✅ 역할을 하다/수행하다/담당하다 (NOT 만들다 ❌)

//**Vocabulary level assessment:**
//✅ GOOD RATIO for Level 5-6:
//- Level 5-6 words: 30-40% (CRITICAL!)
//- Level 3-4 words: 50-60%
//- Level 1-2 words: 10-20%

//⚠️ If 70%+ vocabulary is Level 1-4 → ""Từ vựng quá đơn giản cho Level 5-6"" → Lose 5-10 pts

//=== STEP 6: ESSENTIAL GRAMMAR PATTERNS ===

//These patterns are EXPECTED in Level 5-6 essays:

//**1. DEFINITION (for explanatory essays):**
//N(이)란 [explanation] N/것이다
//Example: 창의력이란 새로운 방식으로 문제를 해결하는 능력이다

//**2. CHARACTERISTICS:**
//A/V-(으)ㄴ/는 것은 A/V-(으)ㄴ/는 것이다
//Example: 이 문제의 심각한 점은 모든 사람에게 영향을 미친다는 것이다

//**3. NECESSITY (very common in problem-solving):**
//V-기 위해서는 -아/어야 하다 / N이/가 필요하다
//Example: 환경을 보호하기 위해서는 모두의 노력이 필요하다

//**4. CAUSATION:**
//-기 때문이다, -기 때문에, -(으)므로, -(으)ㄴ/는 까닭에
//Example: 교육이 중요하기 때문이다

//**5. CONTRAST:**
//-(으)ㄴ/는 반면, 이와 달리, 그러나/하지만
//Example: A는 긍정적인 반면, B는 부정적이다

//**6. REFERENCE/SUMMARY (for conclusion):**
//앞에서 말한 바와 같이, 이상에서 살펴본 바와 같이, 위에서 언급한 것처럼
//Example: 앞에서 말한 바와 같이 환경 보호는 모두의 책임이다

//**7. ENUMERATION:**
//첫째/둘째/셋째, 먼저/다음으로/마지막으로, 한편으로는/다른 한편으로는
//Example: 해결 방법은 다음과 같다. 첫째, 교육을 강화해야 한다. 둘째, 법을 제정해야 한다.

//=== STEP 7: TOP 10 COMMON ERRORS IN TOPIK 54 ===

//**ERROR #1: MISSING SUB-QUESTIONS (30% of errors)**
//Impact: 15-20 points lost
//Example: Only answers Q1 and Q2, ignores Q3
//Fix: ""THIẾU NGHIÊM TRỌNG: Chưa trả lời câu hỏi 3 '[quote]'""

//**ERROR #2: WRONG WRITING STYLE (20% of errors)**
//Impact: 10-20 points lost
//Example: Uses -습니다/-아요 instead of -다/-는다
//Fix: ""SAI VĂN PHONG: Dùng văn nói thay vì văn viết""

//**ERROR #3: LENGTH ISSUES (15% of errors)**
//Impact: 5-15 points lost
//Example: Only 480 characters
//Fix: ""Quá ngắn: 480 ký tự (cần 600-700)""

//**ERROR #4: OFF-TOPIC (12% of errors)**
//Impact: Could fail (0-20 points)
//Example: Question about environment, writes about Korean learning
//Fix: ""LẠC ĐỀ: Đề hỏi [A] nhưng viết về [B]""

//**ERROR #5: POOR STRUCTURE (10% of errors)**
//Impact: 5-10 points lost
//Example: No paragraphs, one block of text
//Fix: ""Thiếu cấu trúc đoạn văn""

//**ERROR #6: BASIC VOCABULARY (8% of errors)**
//Impact: 5-8 points lost
//Example: Only Level 1-3 words
//Fix: ""Từ vựng quá đơn giản - thiếu từ Level 5-6""

//**ERROR #7: PLAGIARISM FROM PROMPT (5% of errors)**
//Impact: Severe - 10-20 points or disqualified
//Example: Copies entire question as intro
//Fix: ""Vi phạm: Copy nguyên văn câu hỏi""

//**ERROR #8: NO TRANSITIONS (5% of errors)**
//Impact: 3-7 points lost
//Example: Each sentence separate, no flow
//Fix: ""Thiếu mạch lạc - không có từ nối""

//**ERROR #9: WRONG COLLOCATIONS (3% of errors)**
//Impact: 2-5 points lost
//Example: 관계를 만들다 instead of 형성하다
//Fix: ""Lỗi kết hợp từ: phải dùng '형성하다'""

//**ERROR #10: REPETITION (2% of errors)**
//Impact: 2-4 points lost
//Example: Uses 중요하다 8 times
//Fix: ""Lặp từ '중요하다' [X] lần - thay bằng 필수적이다, 핵심적이다""

//=== VALIDATION CHECKLIST ===

//**[1] CONTENT (35% = 17.5 points)**
//✅ All sub-questions answered? (each missing = -6 to -8 pts)
//✅ Each answer has 2-3 supporting points?
//✅ Relevant examples provided?
//✅ On-topic throughout?
//✅ No plagiarism from prompt?
//✅ Appropriate depth for Level 5-6?

//Score guide:
//- 15-17.5: Fully addresses all with depth
//- 11-14: Addresses most, some lack depth
//- 6-10: Missing one OR very superficial
//- 0-5: Missing 2+ OR off-topic

//**[2] STRUCTURE (35% = 17.5 points)**
//✅ Clear introduction?
//✅ Organized body (2-3 paragraphs)?
//✅ Logical conclusion?
//✅ Smooth transitions between paragraphs?
//✅ Ideas flow logically?
//✅ Appropriate connectors?

//Score guide:
//- 15-17.5: Perfect structure, seamless
//- 11-14: Good structure, minor issues
//- 6-10: Weak structure, choppy
//- 0-5: No clear structure

//**[3] LANGUAGE (30% = 15 points)**

//**[3a] Writing Style (10 points):**
//✅ ALL sentences use -다/-는다/-았다?
//- 9-10: Perfect, no violations
//- 6-8: 1-2 sentences wrong
//- 3-5: 3-5 sentences wrong
//- 0-2: 6+ wrong or all wrong

//**[3b] Grammar (5 points):**
//✅ No major errors?
//✅ Variety of patterns?
//✅ Selected grammar used correctly?
//- 4-5: No errors, sophisticated
//- 3: 1-2 minor errors
//- 1-2: 3-5 errors or 1 major
//- 0: Numerous errors

//**[3c] Vocabulary (5 points):**
//✅ Level 5-6 vocab (30%+)?
//✅ Correct collocations?
//✅ No excessive repetition?
//- 4-5: Advanced vocab, correct
//- 3: Mostly intermediate, 1-2 errors
//- 1-2: Basic vocab dominates
//- 0: Only beginner level

//**TOTAL SCORING:**
//- 45-50: Excellent (Level 6)
//- 40-44: Good (Level 5)
//- 30-39: Adequate (Level 4)
//- 20-29: Weak (Level 3)
//- 0-19: Poor (below Level 3)

//=== OUTPUT JSON STRUCTURE ===

//{{
//  ""missingInfo"": [
//    ""vietnamese - PRIORITIZED LIST of ALL issues:"",
//    ""1. [CRITICAL - 15-20 pt impact]"",
//    ""   - Thiếu câu trả lời cho câu hỏi [X]: '[quote question]'"",
//    ""   - SAI VĂN PHONG: Dùng -습니다 ở [X] câu"",
//    ""2. [MAJOR - 8-14 pt impact]"",
//    ""   - Bài viết quá ngắn/dài: [X] ký tự"",
//    ""   - Thiếu đoạn [intro/body/conclusion]"",
//    ""3. [MODERATE - 4-7 pt impact]"",
//    ""   - Từ vựng quá đơn giản (Level 1-3)"",
//    ""   - Thiếu từ nối giữa đoạn"",
//    ""4. [MINOR - 1-3 pt impact]"",
//    ""   - Lặp từ '[word]' [X] lần"",
//    ""   - Lỗi kết hợp từ: [specific error]"",
//    ""Ước tính điểm: [X-Y]/50""
//  ],
  
//  ""targetCheck"": {{
//    ""grammar_used"": [
//      ""korean - Grammar user ACTUALLY used correctly"",
//      ""-기 때문이다 (used 2 times, correct)"",
//      ""V-아/어야 하다 (used correctly for necessity)""
//    ],
//    ""grammar_not_used"": [
//      ""korean - Appropriate grammar NOT used (missed opportunity)"",
//      ""-(으)ㄴ 반면 (selected but not used - could compare viewpoints)"",
//      ""앞에서 말한 바와 같이 (good for conclusion)""
//    ],
//    ""grammar_not_applicable"": [
//      ""korean - Grammar selected but NOT suitable for this essay"",
//      ""-(으)려고 하다 (personal intention, not for academic argument)""
//    ]
//  }},
  
//  ""feedback"": {{
//    ""overall"": [
//      ""vietnamese - COMPREHENSIVE SUMMARY:"",
//      ""Độ dài: [X] ký tự ([đạt/thiếu/thừa])"",
//      ""Cấu trúc: [có đủ/thiếu] mở bài-thân bài-kết luận"",
//      ""Trả lời câu hỏi: [X]/[Y] câu hỏi phụ"",
//      ""Văn phong: [Đúng/Sai [X] câu/Sai hoàn toàn]"",
//      ""Từ vựng: Level [X-Y], [phù hợp/chưa phù hợp] Level 5-6"",
//      ""Ngữ pháp: [X] lỗi [nhỏ/vừa/nghiêm trọng]"",
//      ""Ước tính điểm: [X-Y]/50""
//    ],
    
//    ""coherence"": [
//      ""vietnamese - STRUCTURE & LOGIC:"",
//      ""Cấu trúc đoạn: [rõ ràng/không rõ] - [lý do]"",
//      ""Liên kết đoạn: [mượt mà/thiếu/đột ngột]"",
//      ""Trình tự logic: [hợp lý/chưa hợp lý] - [giải thích]"",
//      ""Phát triển ý: [đầy đủ/thiếu chi tiết/chung chung]"",
//      ""Ví dụ: [cụ thể/chung chung/không có]""
//    ],
    
//    ""grammar"": [
//      ""vietnamese - DETAILED ANALYSIS:"",
//      """",
//      ""📝 VĂN PHONG (10/15 points):"",
//      ""  - Kiểm tra: [All/Most/Some/None] câu dùng đúng -다/-는다"",
//      ""  - Vi phạm: [List sentences with -습니다/-아요]"",
//      ""  - Đánh giá: [Excellent/Good/Poor]"",
//      """",
//      ""📝 NGỮ PHÁP CẤU TRÚC (5/15 points):"",
//      ""  - Đã dùng: [List patterns used correctly]"",
//      ""  - Chưa dùng: [List missing helpful patterns]"",
//      ""  - Dùng sai: [List incorrect usage]"",
//      """",
//      ""📝 TỪ VỰNG (5/15 points):"",
//      ""  - Mức độ: [%] Level 5-6 - [assessment]"",
//      ""  - Từ tốt: [List advanced words]"",
//      ""  - Cần thay: [basic → advanced]"",
//      ""  - Kết hợp từ sai: [collocation errors]"",
//      """",
//      ""📝 TRỢ TỪ: [particle errors]"",
//      ""📝 CHÍNH TẢ: [spelling errors]"",
//      ""📝 LẶP TỪ: [overused words with count]""
//    ],
    
//    ""corrections"": [
//      {{
//        ""original"": ""korean - EXACT problematic text from user"",
//        ""fixed"": ""korean - CORRECTED version"",
//        ""reason_vi"": ""vietnamese - DETAILED explanation: (1) What's wrong? (2) Why wrong? (3) Rule? (4) How to fix?""
//      }}
//    ]
//  }},
  
//  ""polishedAnswers"": {{
//    ""blank1"": ""korean - COMPLETE REVISED ESSAY (620-680 chars ideal) with:"",
//    ""  - ALL sub-questions answered with depth (2-3 points each)"",
//    ""  - Clear structure: Intro (100-150) + Body1 (200-250) + Body2 (200-250) + Conclusion (100-150)"",
//    ""  - Correct writing style (-다/-는다/-았다) throughout"",
//    ""  - Advanced vocabulary (30%+ Level 5-6)"",
//    ""  - Smooth transitions (먼저, 다음으로, 마지막으로, 따라서)"",
//    ""  - Logical development with specific examples"",
//    ""  - Proper collocations (형성하다, 기울이다, 미치다, 인식하다)"",
//    ""  - No repetition, no plagiarism from prompt"",
//    ""  - Essential patterns: -기 위해서는, -(으)로 인해, 앞에서 말한 바와 같이"",
//    """",
//    ""blank2"": null
//  }}
//}}

//=== CRITICAL FINAL REMINDERS ===

//1. ⚠️ READ SUB-QUESTIONS CAREFULLY - Missing ONE = 15-20 pt loss
//2. ⚠️ CHECK WRITING STYLE FIRST - Wrong style = fail
//3. ⚠️ COUNT CHARACTERS - <550 or >750 = immediate deduction
//4. ⚠️ VERIFY STRUCTURE - Must have intro + body (2-3¶) + conclusion
//5. ⚠️ ASSESS VOCABULARY - Must use 30%+ Level 5-6 words
//6. ⚠️ CHECK PLAGIARISM - Don't copy question (5+ words = violation)
//7. ⚠️ EVALUATE LOGIC - Ideas must connect with transitions
//8. ⚠️ PROVIDE COMPLETE REVISION - polishedAnswers = FULL essay (not summary!)
//9. ⚠️ PRIORITIZE ERRORS - List critical first (missing sub-Q, wrong style)
//10. ⚠️ BE SPECIFIC - Quote exact errors, explain exact fixes

//This is the HIGHEST-POINT question in TOPIK II (50/100 writing points).
//Grade with UTMOST RIGOR and PRECISION.
//";
//        }
//        private static string GetJsonSchema(string questionType)
//        {
//            return @"
//=== JSON SCHEMA ===
//{{
//  ""missingInfo"": [""vietnamese - list of issues""],
//  ""targetCheck"": {{
//    ""grammar_used"": [""korean - used correctly & appropriately""],
//    ""grammar_not_used"": [""korean - appropriate but not used""],
//    ""grammar_not_applicable"": [""korean - not applicable to context""]
//  }},
//  ""feedback"": {{
//    ""overall"": [""vietnamese""],
//    ""coherence"": [""vietnamese""],
//    ""grammar"": [""vietnamese - detailed analysis""],
//    ""corrections"": [{{""original"": ""korean"", ""fixed"": ""korean"", ""reason_vi"": ""vietnamese""}}]
//  }},
//  ""polishedAnswers"": {{
//    ""blank1"": ""korean - main answer"",
//    ""blank2"": ""korean or null""
//  }}
//}}

//CRITICAL:
//- ALL feedback in VIETNAMESE
//- Return ONLY JSON, NO markdown backticks
//- For 51/52 with 2 blanks: blank1 and blank2 separate
//- For 53/54: only blank1, blank2 = null
//";
//        }

//        private static string DetermineQuestionType(int questionNo)
//        {
//            return questionNo switch
//            {
//                51 => "TOPIK_51",
//                52 => "TOPIK_52",
//                53 => "TOPIK_53",
//                54 => "TOPIK_54",
//                _ => throw new ArgumentException($"Số câu không hợp lệ: {questionNo}. Phải từ 51-54.")
//            };
//        }

//        private static string GetRequirements(string type)
//        {
//            return type switch
//            {
//                "TOPIK_51" => "Điền 1 CÂU phù hợp ngữ cảnh hội thoại/thông báo, văn phong formal (-습니다/-ㅂ니다)",
//                "TOPIK_52" => "Điền 1 CÂU phù hợp ngữ cảnh đoạn văn, văn phong written (-다/-는다)",
//                "TOPIK_53" => "Viết đoạn văn 200-300 ký tự mô tả biểu đồ/khảo sát, văn phong written (-다/-는다)",
//                "TOPIK_54" => "Viết bài luận 600-700 ký tự với cấu trúc đầy đủ, văn phong written (-다/-는다)",
//                _ => "Unknown"
//            };
//        }

//        private static string GetGradingCriteria(string type)
//        {
//            return type switch
//            {
//                "TOPIK_51" => "Ngữ pháp (40%), Phù hợp ngữ cảnh (40%), Chính tả (20%)",
//                "TOPIK_52" => "Ngữ pháp (40%), Logic/Mạch lạc (40%), Chính tả (20%)",
//                "TOPIK_53" => "Nội dung đầy đủ (40%), Độ chính xác số liệu (30%), Ngữ pháp/Từ vựng (30%)",
//                "TOPIK_54" => "Nội dung/Luận điểm (35%), Cấu trúc/Mạch lạc (35%), Ngữ pháp/Từ vựng cao cấp (30%)",
//                _ => "Unknown"
//            };
//        }
//    }
//}