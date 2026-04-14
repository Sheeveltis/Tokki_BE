using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class GetPronunciationRulesQueryHandlerTests
    {
        private static GetPronunciationRulesQueryHandler CreateHandler(
            Mock<IPronunciationRuleRepository>? repo = null)
        {
            return new GetPronunciationRulesQueryHandler(
                (repo ?? MockPronunciationRuleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-01 | N | Happy path with items → paged result returned, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasItems_ShouldReturnPagedResultWith200()
        {
            // Arrange
            var rules = MockPronunciationRuleRepository.GetSampleRuleList();
            var repo  = MockPronunciationRuleRepository.GetMock(pagedItems: rules, pagedTotal: rules.Count);
            var handler = CreateHandler(repo);
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 20 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(rules.Count);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-01",
                Description       = "Happy path: 3 rules in DB → paged result returned with IsSuccess=true and StatusCode=200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Items.Count=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns 3 items", "PagedResult created", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-02 | N | Empty result → PagedResult with 0 items
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoItems_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(
                pagedItems: new List<Domain.Entities.PronunciationRule>(),
                pagedTotal: 0);
            var handler = CreateHandler(repo);
            var query = new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 20 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-02",
                Description       = "No rules in DB → PagedResult with empty Items and TotalCount=0",
                ExpectedResult    = "IsSuccess=true, Items=[], TotalCount=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns ([], 0)", "PagedResult.Items empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-03 | N | DTO list correctly mapped (RuleName, Description, Content, SortOrder)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasItems_ShouldMapDTOFieldsCorrectly()
        {
            // Arrange
            var rules = MockPronunciationRuleRepository.GetSampleRuleList();
            var repo  = MockPronunciationRuleRepository.GetMock(pagedItems: rules, pagedTotal: rules.Count);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetPronunciationRulesQuery { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            for (int i = 0; i < rules.Count; i++)
            {
                result.Data!.Items[i].PronunciationRuleId.Should().Be(rules[i].PronunciationRuleId);
                result.Data!.Items[i].RuleName.Should().Be(rules[i].RuleName);
                result.Data!.Items[i].SortOrder.Should().Be(rules[i].SortOrder);
            }

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-03",
                Description       = "Each PronunciationRuleDTO correctly mapped from entity (Id, Name, SortOrder)",
                ExpectedResult    = "All DTO fields match source entities",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "3 entities in page", "Each DTO field verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-04 | N | PagedResult metadata correct (TotalCount, PageNumber, PageSize)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasItems_ShouldSetPagedResultMetadataCorrectly()
        {
            // Arrange
            var rules = MockPronunciationRuleRepository.GetSampleRuleList();
            var repo  = MockPronunciationRuleRepository.GetMock(pagedItems: rules, pagedTotal: 50);
            var handler = CreateHandler(repo);
            var query = new GetPronunciationRulesQuery { PageNumber = 2, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalCount.Should().Be(50);
            result.Data!.PageNumber.Should().Be(2);
            result.Data!.PageSize.Should().Be(10);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-04",
                Description       = "PagedResult metadata (TotalCount=50, PageNumber=2, PageSize=10) correctly set",
                ExpectedResult    = "TotalCount=50, PageNumber=2, PageSize=10",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber=2, PageSize=10", "TotalCount passed from repo=50" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-05 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationRuleRepository>();
            repo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB timeout"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new GetPronunciationRulesQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB timeout");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-05",
                Description       = "Repository throws exception on GetPagedAsync → exception propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync throws InvalidOperationException" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GPL-06 | B | SearchTerm passed through to GetPagedAsync
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithSearchTerm_ShouldPassSearchTermToRepository()
        {
            // Arrange
            const string searchTerm = "받침";
            var repo = MockPronunciationRuleRepository.GetMock(
                pagedItems: new List<Domain.Entities.PronunciationRule>(),
                pagedTotal: 0);
            var handler = CreateHandler(repo);
            var query = new GetPronunciationRulesQuery
            {
                PageNumber = 1,
                PageSize   = 20,
                SearchTerm = searchTerm
            };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert: GetPagedAsync called with the exact searchTerm
            repo.Verify(x => x.GetPagedAsync(
                1, 20, searchTerm, It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRules",
                TestCaseID        = "TC-PR-GPL-06",
                Description       = "Boundary: SearchTerm from query is passed through to GetPagedAsync unmodified",
                ExpectedResult    = "GetPagedAsync(1, 20, '받침', ...) called Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchTerm='받침' provided", "Passed verbatim to GetPagedAsync" }
            });
        }
    }
}
