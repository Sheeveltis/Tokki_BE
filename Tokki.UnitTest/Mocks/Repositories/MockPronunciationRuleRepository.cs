using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockPronunciationRuleRepository
    {
        /// <summary>
        /// Creates a configured Mock of IPronunciationRuleRepository.
        /// </summary>
        /// <param name="getByIdResult">Result returned by GetByIdAsync</param>
        /// <param name="ruleNameExists">Result returned by IsRuleNameExistsAsync</param>
        /// <param name="pagedItems">Items returned by GetPagedAsync</param>
        /// <param name="pagedTotal">Total count returned by GetPagedAsync</param>
        public static Mock<IPronunciationRuleRepository> GetMock(
            PronunciationRule? getByIdResult = null,
            bool ruleNameExists              = false,
            List<PronunciationRule>? pagedItems = null,
            int pagedTotal                   = 0)
        {
            var mockRepo = new Mock<IPronunciationRuleRepository>();

            // GetByIdAsync — used by Delete, Update, GetById, Evaluate
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(getByIdResult);

            // GetByIdWithDetailsAsync — currently unused by tested handlers, stub anyway
            mockRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>()))
                    .ReturnsAsync(getByIdResult);

            // IsRuleNameExistsAsync — used by Create and Update
            mockRepo.Setup(x => x.IsRuleNameExistsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string?>()))
                    .ReturnsAsync(ruleNameExists);

            // AddAsync — used by Create
            mockRepo.Setup(x => x.AddAsync(It.IsAny<PronunciationRule>()))
                    .Returns(Task.CompletedTask);

            // UpdateAsync — used by Update
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<PronunciationRule>()))
                    .Returns(Task.CompletedTask);

            // DeleteAsync — used by Delete
            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<PronunciationRule>()))
                    .Returns(Task.CompletedTask);

            // SaveChangesAsync — used by Create, Update, Delete
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

            // GetPagedAsync — used by GetPronunciationRules
            mockRepo.Setup(x => x.GetPagedAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((pagedItems ?? new List<PronunciationRule>(), pagedTotal));

            // GetAllActiveRulesAsync — stub
            mockRepo.Setup(x => x.GetAllActiveRulesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<PronunciationRule>());

            return mockRepo;
        }

        // ===== Sample Data =====

        public static PronunciationRule GetSampleRule(
            string ruleId  = "RULE-001",
            string ruleName = "받침 발음",
            int sortOrder  = 1)
        {
            return new PronunciationRule
            {
                PronunciationRuleId = ruleId,
                RuleName            = ruleName,
                Description         = "Final consonant pronunciation rules in Korean",
                Content             = "When ㅂ, ㄱ, ㄷ appear at the end of syllable...",
                IsDeleted           = false,
                CreateBy            = "ADMIN-001",
                SortOrder           = sortOrder
            };
        }

        public static List<PronunciationRule> GetSampleRuleList()
        {
            return new List<PronunciationRule>
            {
                new PronunciationRule
                {
                    PronunciationRuleId = "RULE-001",
                    RuleName            = "받침 발음",
                    Description         = "Final consonant rule",
                    Content             = "Content 1",
                    SortOrder           = 1
                },
                new PronunciationRule
                {
                    PronunciationRuleId = "RULE-002",
                    RuleName            = "연음 법칙",
                    Description         = "Liaison rule",
                    Content             = "Content 2",
                    SortOrder           = 2
                },
                new PronunciationRule
                {
                    PronunciationRuleId = "RULE-003",
                    RuleName            = "경음화",
                    Description         = "Fortis rule",
                    Content             = "Content 3",
                    SortOrder           = 3
                }
            };
        }
    }
}
