using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockPronunciationExampleRepository
    {
        /// <summary>
        /// Creates a configured Mock of IPronunciationExampleRepository.
        /// </summary>
        /// <param name="getByIdResult">Result returned by GetByIdAsync</param>
        /// <param name="getDetailByIdResult">Result returned by GetDetailByIdAsync</param>
        /// <param name="getByRuleIdResult">Result returned by GetExamplesByRuleIdAsync</param>
        public static Mock<IPronunciationExampleRepository> GetMock(
            PronunciationExample? getByIdResult = null,
            PronunciationExample? getDetailByIdResult = null,
            List<PronunciationExample>? getByRuleIdResult = null)
        {
            var mockRepo = new Mock<IPronunciationExampleRepository>();

            // GetByIdAsync — used by EvaluatePronunciation
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(getByIdResult);

            // GetDetailByIdAsync — used by GetExampleDetail
            mockRepo.Setup(x => x.GetDetailByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(getDetailByIdResult);

            // GetExamplesByRuleIdAsync — used by GetExamplesByRuleId
            mockRepo.Setup(x => x.GetExamplesByRuleIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(getByRuleIdResult ?? new List<PronunciationExample>());

            // AddRangeAsync — used by ImportPronunciationExample
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PronunciationExample>>()))
                    .Returns(Task.CompletedTask);

            return mockRepo;
        }

        // ===== Sample Data =====

        public static PronunciationExample GetSampleExample(
            string exampleId = "EX-0001",
            string ruleId    = "RULE-001",
            string rawScript = "안녕하세요")
        {
            return new PronunciationExample
            {
                ExampleId           = exampleId,
                PronunciationRuleId = ruleId,
                TargetScript        = "안녕하세요",
                RawScript           = rawScript,
                PhoneticScript      = "an-nyeong-ha-se-yo",
                Meaning             = "Hello",
                AudioUrl            = "https://cdn.tokki.com/audio/ex001.mp3",
                SortOrder           = 1,
                IsDeleted           = false,
                CreateBy            = "ADMIN-001"
            };
        }

        public static PronunciationExample GetSampleExampleWithRule(
            string exampleId = "EX-0002",
            string ruleId    = "RULE-001")
        {
            return new PronunciationExample
            {
                ExampleId           = exampleId,
                PronunciationRuleId = ruleId,
                TargetScript        = "감사합니다",
                RawScript           = "감사합니다",
                PhoneticScript      = "gam-sa-ham-ni-da",
                Meaning             = "Thank you",
                AudioUrl            = "https://cdn.tokki.com/audio/ex002.mp3",
                SortOrder           = 2,
                PronunciationRule   = new Domain.Entities.PronunciationRule
                {
                    PronunciationRuleId = ruleId,
                    RuleName            = "받침 발음",
                    Description         = "Final consonant pronunciation rule",
                    Content             = "When ㅂ appears at end of syllable...",
                    SortOrder           = 1
                }
            };
        }

        public static List<PronunciationExample> GetSampleExampleList(string ruleId = "RULE-001")
        {
            return new List<PronunciationExample>
            {
                new PronunciationExample
                {
                    ExampleId           = "EX-0001",
                    PronunciationRuleId = ruleId,
                    RawScript           = "안녕하세요",
                    SortOrder           = 1
                },
                new PronunciationExample
                {
                    ExampleId           = "EX-0002",
                    PronunciationRuleId = ruleId,
                    RawScript           = "감사합니다",
                    SortOrder           = 2
                },
                new PronunciationExample
                {
                    ExampleId           = "EX-0003",
                    PronunciationRuleId = ruleId,
                    RawScript           = "반갑습니다",
                    SortOrder           = 3
                }
            };
        }
    }
}
