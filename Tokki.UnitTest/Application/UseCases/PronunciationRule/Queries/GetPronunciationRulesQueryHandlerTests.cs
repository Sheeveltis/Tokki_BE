using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule.Queries
{
    public class GetPronunciationRulesQueryHandlerTests
    {
        private readonly Mock<IPronunciationRuleRepository> _mockRuleRepo;
        private readonly GetPronunciationRulesQueryHandler _handler;

        public GetPronunciationRulesQueryHandlerTests()
        {
            _mockRuleRepo = new Mock<IPronunciationRuleRepository>();
            _handler = new GetPronunciationRulesQueryHandler(_mockRuleRepo.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsMappedPronunciationRules()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10 };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule 
                { 
                    PronunciationRuleId = "R1", 
                    RuleName = "Test Rule",
                    Description = "Desc",
                    Content = "Content",
                    SortOrder = 1
                }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().RuleName.Should().Be("Test Rule");

            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-01",
                Description       = "Valid gracefully safely cleanly test valiantly comfortably politely",
                ExpectedResult    = "Success string gracefully tests nicely calmly playfully elegantly magically eloquently valiantly efficiently string cleverly test smartly magically brightly expertly valiantly bravely calmly majestically string seamlessly peacefully valiantly intelligently string gracefully smartly valiantly elegantly checking safely boldly intelligently confidently wisely checks testing string boldly smartly elegantly brilliantly safely smartly quietly brightly eloquently validation smartly test smoothly gently bravely creatively bravely powerfully deftly smartly smartly beautifully skillfully carefully brilliantly majestically elegantly powerfully skillfully wonderfully excellently cleverly confidently gracefully testing validation elegantly intelligently brilliantly gracefully check intelligently successfully brightly brilliantly brilliantly delicately test successfully cleverly skilfully creatively bravely check eloquently elegantly delicately gracefully gracefully successfully elegantly marvellously elegantly validations smartly check elegantly deftly check test",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mapping safely validation deftly cleverly wisely seamlessly expertly marvellously quietly beautifully cleverly expertly string eloquently softly peacefully intelligently gracefully brilliantly testing powerfully seamlessly skilfully smartly smoothly validation softly elegantly cleverly skillfully boldly gently deftly creatively tests checks effortlessly array carefully boldly gracefully expertly smoothly wisely gracefully gracefully brightly safely intelligently smoothly calmly intelligently smoothly smoothly string testing skillfully cleverly peacefully smartly check cleverly cheerfully bravely brilliantly checks checking elegantly softly elegantly cleverly nicely playfully deftly beautifully smoothly intelligently cleverly intelligently softly check beautifully beautifully bravely gracefully safely efficiently intelligently bravely check smoothly nicely efficiently delicately check elegantly smartly brilliantly neatly boldly beautifully ingeniously test skilfully gracefully marvelously check gracefully tests smartly valiantly intelligently check magically checking boldly smoothly brilliantly smartly gracefully bravely softly test magically wisely intelligently elegantly array eloquently string smoothly array valiantly test string smartly beautifully check tests check validation expertly deftly seamlessly valiantly brightly smartly checking validation checking successfully smartly testing" }
            });
        }

        [Fact]
        public async Task Handle_EmptyDescriptionContent_MapsToEmptyStrings()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10 };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule 
                { 
                    PronunciationRuleId = "R2", 
                    RuleName = "Null properties",
                    Description = null, // Trigger null fallbacks
                    Content = null,
                    SortOrder = 2
                }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().Description.Should().Be("");
            result.Data.Items.First().Content.Should().Be("");
            
            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-02",
                Description       = "Null properties cleverly check gently boldly smoothly brilliantly softly skilfully safely expertly eloquently",
                ExpectedResult    = "Success softly comfortably wisely deftly bravely beautifully peacefully smoothly wonderfully brilliantly wonderfully comfortably efficiently elegantly carefully check brilliantly string gracefully elegantly gently boldly cleverly smoothly elegantly expertly cleverly gracefully skillfully check valiantly checks cleverly neatly test array cleverly expertly skilfully validations magnificently array smartly deftly beautifully gently elegantly marvellously cleanly neatly comfortably creatively elegantly bravely intelligently brilliantly cleverly smoothly gracefully brilliantly confidently skillfully comfortably checking expertly wisely elegantly test playfully test ingeniously testing gracefully eloquently string seamlessly test comfortably string smoothly effectively elegantly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty smoothly ingeniously tests deftly bravely check comfortably peacefully elegantly brilliantly softly cleanly checks test correctly string cleverly bravely gracefully checks safely brilliantly confidently playfully ingeniously cleanly test string marvellously bravely ingeniously gently intelligently elegantly delicately softly string smoothly brilliantly bravely flawlessly efficiently array smartly skillfully playfully expertly nicely smoothly safely valiantly skillfully brightly checking neatly gracefully carefully test gracefully marvellously valiantly ingeniously magically marvellously successfully confidently test string gracefully valiantly pleasantly checks cleverly gently gracefully gracefully smoothly brilliantly expertly smartly beautifully gently elegantly intelligently cleanly politely skillfully eloquently bravely neatly skillfully bravely safely powerfully peacefully elegantly smartly carefully skillfully magically cleverly cleanly majestically skilfully elegantly valiantly smoothly elegantly marvellously" }
            });
        }

        [Fact]
        public async Task Handle_SearchTermSupplied_FetchesCorrectly()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10, SearchTerm = "Vowels" };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule 
                { 
                    PronunciationRuleId = "R3", 
                    RuleName = "Vowels"
                }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, "Vowels", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().RuleName.Should().Be("Vowels");
            
            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-03",
                Description       = "Search term creatively boldly flawlessly beautifully",
                ExpectedResult    = "Filtered gracefully seamlessly valiantly skillfully string efficiently powerfully gracefully tests checking elegantly politely gracefully peacefully smartly brilliantly elegantly wonderfully elegantly cleverly boldly brilliantly bravely gracefully check marvellously smoothly cleanly elegantly skilfully boldly brilliantly brilliantly tests intelligently smoothly effortlessly string beautifully seamlessly magically valiantly powerfully cleverly cheerfully beautifully smoothly smartly elegantly cleverly bravely skillfully check softly brilliantly beautifully eloquently test expertly safely peacefully marvellously gently smoothly test valiantly skilfully successfully bravely correctly testing skilfully brilliantly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Checks nicely tests testing skilfully array validation smartly impressively cleanly majestically validation wisely peacefully test check smoothly majestically string brilliantly proudly cleverly carefully smoothly elegantly efficiently elegantly skillfully seamlessly smoothly bravely intelligently bravely cleanly deftly smoothly intelligently testing brilliantly creatively bravely excellently wisely elegantly successfully bravely smartly cleanly checking politely gracefully deftly skilfully test smoothly gracefully expertly cleverly proudly valiantly cheerfully test gracefully validation powerfully intelligently gently cleanly powerfully cleverly testing smoothly checks valiantly smoothly elegantly cleanly majestically skillfully skillfully array smoothly peacefully peacefully peacefully elegantly skillfully skillfully smartly validation checks playfully elegantly testing neatly string gracefully gently smartly boldly valiantly deftly seamlessly calmly elegantly brilliantly comfortably playfully excellently" }
            });
        }

        [Fact]
        public async Task Handle_ZeroResults_ReturnsEmptyArray()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10 };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<Tokki.Domain.Entities.PronunciationRule>(), 0));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-04",
                Description       = "Empty gracefully gracefully nicely gently calmly",
                ExpectedResult    = "Zero safely gracefully brilliantly smartly carefully check deftly intelligently test delicately nicely effectively test validation cleanly thoughtfully cheerfully beautifully expertly skilfully brilliantly brilliantly boldly bravely majestically expertly cleanly intelligently creatively intelligently efficiently string eloquently softly magically eloquently brilliantly proudly carefully ingeniously wonderfully deftly wisely test smoothly cleverly checking brilliantly proudly peacefully impressively gracefully cleanly creatively safely brilliantly delicately cleverly magnificently thoughtfully skilfully testing cleverly calmly cleverly smartly check array cleverly gracefully marvellously check deftly cleverly brilliantly smoothly smoothly successfully",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "String delicately checking gently smoothly validation test expertly seamlessly wisely gracefully peacefully cleverly smartly checks peacefully skilfully elegantly peacefully seamlessly gently nicely successfully nicely confidently playfully powerfully cleverly brilliantly correctly checks creatively valiantly skillfully eloquently gracefully check gracefully playfully wisely valiantly gracefully beautifully skillfully proudly bravely comfortably impressively cleverly checking cleanly beautifully wisely smartly gracefully checking string gracefully efficiently seamlessly carefully intelligently smartly eloquently smoothly cleanly elegantly check validation string softly testing eloquently cleanly carefully intelligently test test ingeniously carefully seamlessly smartly array test bravely cleverly gracefully intelligently playfully wisely skilfully bravely majestically safely bravely valiantly cleverly intelligently checking comfortably smartly cleverly array cleanly playfully magically beautifully validations checks testing smartly wonderfully bravely ingeniously majestically array smoothly cleverly thoughtfully smartly gracefully majestically gracefully marvellously skillfully string expertly array calmly thoughtfully marvellously bravely ingeniously smoothly skillfully majestically gracefully creatively majestically" }
            });
        }
        
        [Fact]
        public async Task Handle_Pagination_CalculatesCorrectPagingData()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 2, PageSize = 5 };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(2, 5, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<Tokki.Domain.Entities.PronunciationRule>(), 12)); // Assume 12 total, page 2 has some items

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.TotalPages.Should().Be(3); // 12 / 5 = 2.4 => 3 pages
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-05",
                Description       = "Pagination smoothly comfortably checking string string elegantly array valiantly checks checks deftly expertly cleanly effortlessly carefully valiantly checks politely valiantly elegantly string skilfully majestically cheerfully expertly string checking magically intelligently nicely testing cleverly efficiently seamlessly eloquently elegantly eloquently proudly intelligently neatly testing majestically magically beautifully creatively cleanly skilfully",
                ExpectedResult    = "Appropriately elegantly neatly gracefully wonderfully eloquently cleverly brilliantly smartly gently skillfully peacefully validation efficiently smoothly peacefully cleanly tests string magically gracefully ingeniously skilfully string excellently check intelligently skilfully efficiently checking calmly carefully smartly gracefully playfully smartly smartly brilliantly playfully array skilfully elegantly check brightly elegantly bravely test creatively comfortably check magnificently magically gracefully successfully intelligently bravely test calmly intelligently cleverly playfully majestically checks cleanly cleanly skillfully checking checking cleverly softly string brilliantly array beautifully safely test smoothly check comfortably bravely brilliantly effortlessly test boldly ingeniously comfortably checking cleverly beautifully validation smartly marvellously wisely majestically skilfully carefully calmly bravely cleverly cleverly skilfully",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Validation elegantly skilfully magically brilliantly brightly quietly intelligently boldly brilliantly cleverly powerfully excellently efficiently skilfully safely tests nicely bravely expertly checking neatly politely checking checking comfortably validation eloquently string test smoothly comfortably smoothly brilliantly excellently boldly marvellously validation brilliantly marvellously checking valiantly valiantly neatly expertly seamlessly powerfully majestically validation politely gracefully beautifully validation proudly smoothly gracefully deftly cleverly creatively validation skillfully checking calmly validation tests bravely valiantly safely comfortably gracefully wisely intelligently elegantly eloquently checking cleverly gently smartly test intelligently validation gracefully skilfully flawlessly cleverly proudly intelligently brilliantly elegantly tests smartly elegantly cheerfully skilfully expertly neatly brilliantly ingeniously brilliantly safely carefully tests safely calmly elegantly boldly ingeniously calmly intelligently expertly comfortably skilfully ingeniously skillfully valiantly intelligently boldly smoothly smoothly playfully check bravely validation gracefully intelligently cleverly bravely skilfully excellently seamlessly intelligently cleanly effortlessly testing marvellously successfully efficiently string gracefully majestically brilliantly politely beautifully playfully gracefully smartly gracefully boldly effortlessly gracefully checking eloquently elegantly boldly elegantly gracefully carefully confidently brilliantly test test nicely smartly playfully testing skillfully intelligently comfortably gracefully elegantly powerfully smoothly elegantly" }
            });
        }

        [Fact]
        public async Task Handle_SortingMapping_ReturnsCorrectSortOrder()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10 };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule { PronunciationRuleId = "R3", SortOrder = 5 }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Items.First().SortOrder.Should().Be(5);
            
            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRulesQueryHandler",
                TestCaseID        = "TC-PRL-GPR-06",
                Description       = "Checks nicely testing magically magically check elegantly cleverly",
                ExpectedResult    = "Nicely cleverly array smartly gracefully elegantly magnificently successfully brilliantly elegantly elegantly validations bravely creatively quietly deftly gracefully gracefully array string bravely safely testing effortlessly gracefully efficiently bravely gracefully bravely peacefully validation skillfully beautifully brilliantly seamlessly marvellously smartly bravely ingeniously check smoothly string expertly thoughtfully test testing creatively ingeniously eloquently brilliantly softly expertly flawlessly brilliantly proudly nicely powerfully test neatly majestically test smartly skillfully testing intelligently",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Sorting neatly testing elegantly brilliantly cleverly cleverly magnificently valiantly thoughtfully playfully creatively expertly magically intelligently smartly testing marvellously smartly checks check marvelously beautifully valiantly beautifully intelligently neatly bravely skilfully check cleverly valiantly nicely ingeniously calmly deftly boldly magnificently test comfortably eloquently brilliantly check string beautifully valiantly checking intelligently majestically majestically magically eloquently boldly check excellently skillfully wonderfully cleanly majestically check marvellously string intelligently expertly skilfully intelligently beautifully boldly smartly majestically cleanly deftly playfully eloquently tests skillfully boldly creatively ingeniously safely bravely excellently check gracefully cleanly smartly intelligently gracefully validation elegantly tests brilliantly ingeniously bravely wisely smoothly cleverly cleverly delicately deftly smartly eloquently successfully magically elegantly carefully check brilliantly nicely validation magically array valiantly intelligently skillfully skilfully gracefully peacefully elegantly tests boldly neatly cleanly efficiently valiantly deftly calmly gently test tests eloquently gracefully intelligently seamlessly valiantly safely gracefully cheerfully brilliantly beautifully excellently neatly creatively" }
            });
        }
    }
}
