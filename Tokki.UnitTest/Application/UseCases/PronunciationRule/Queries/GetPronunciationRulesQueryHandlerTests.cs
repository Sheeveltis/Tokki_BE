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
                    PronunciationRuleId ="R1", 
                    RuleName ="Test Rule",
                    Description ="Desc",
                    Content ="Content",
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
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_01",
                Description       ="Valid",
                ExpectedResult    ="Success",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Mapping" }
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
                    PronunciationRuleId ="R2", 
                    RuleName ="Null properties",
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
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_02",
                Description       ="Null",
                ExpectedResult    ="Success",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty" }
            });
        }

        [Fact]
        public async Task Handle_SearchTermSupplied_FetchesCorrectly()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10, SearchTerm ="Vowels" };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule 
                { 
                    PronunciationRuleId ="R3", 
                    RuleName ="Vowels"
                }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10,"Vowels", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().RuleName.Should().Be("Vowels");
            
            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_03",
                Description       ="Search term",
                ExpectedResult    ="Filtered",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Checks" }
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
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_04",
                Description       ="Empty",
                ExpectedResult    ="Zero",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"String" }
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
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_05",
                Description       ="Pagination",
                ExpectedResult    ="Appropriately",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Validation" }
            });
        }

        [Fact]
        public async Task Handle_SortingMapping_ReturnsCorrectSortOrder()
        {
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 10 };
            
            var data = new List<Tokki.Domain.Entities.PronunciationRule>
            {
                new Tokki.Domain.Entities.PronunciationRule { PronunciationRuleId ="R3", SortOrder = 5 }
            };

            _mockRuleRepo.Setup(x => x.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Items.First().SortOrder.Should().Be(5);
            
            QACollector.LogTestCase("PronunciationRule - Get", new TestCaseDetail
            {
                FunctionGroup     ="GetPronunciationRulesQueryHandler",
                TestCaseID        ="GetPronunciationRulesQueryHandler_06",
                Description       ="",
                ExpectedResult    ="Nicely",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Sorting" }
            });
        }
    }
}
