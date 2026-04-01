using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRuleById;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class GetPronunciationRuleByIdQueryHandlerTests
    {
        private static GetPronunciationRuleByIdQueryHandler CreateHandler(
            Mock<IPronunciationRuleRepository>? repo = null)
        {
            return new GetPronunciationRuleByIdQueryHandler(
                (repo ?? MockPronunciationRuleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-01 | A | RuleId not found → 404 Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleNotFound_ShouldReturn404Failure()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(repo);
            var query = new GetPronunciationRuleByIdQuery("RULE-NOTEXIST");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-01",
                Description       = "PronunciationRuleId does not exist → return 404 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 Failure returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-02 | N | Happy path → DTO correctly mapped
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleFound_ShouldReturnMappedDTO()
        {
            // Arrange
            var rule = MockPronunciationRuleRepository.GetSampleRule("RULE-001", "받침 발음", sortOrder: 3);
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetPronunciationRuleByIdQuery("RULE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PronunciationRuleId.Should().Be(rule.PronunciationRuleId);
            result.Data!.RuleName.Should().Be(rule.RuleName);
            result.Data!.Description.Should().Be(rule.Description);
            result.Data!.Content.Should().Be(rule.Content);
            result.Data!.SortOrder.Should().Be(rule.SortOrder);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-02",
                Description       = "Happy path: rule found → PronunciationRuleDTO correctly mapped from entity",
                ExpectedResult    = "All DTO fields match entity properties",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns entity", "DTO fully mapped" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-03 | N | Result has default 200 status (Success without explicit code)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleFound_ShouldReturnSuccess()
        {
            // Arrange
            var rule = MockPronunciationRuleRepository.GetSampleRule();
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetPronunciationRuleByIdQuery(rule.PronunciationRuleId), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-03",
                Description       = "Rule found → result IsSuccess=true",
                ExpectedResult    = "IsSuccess=true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid RuleId", "OperationResult.Success returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-04 | N | Description is null → empty string in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullDescription_ShouldReturnEmptyStringInDTO()
        {
            // Arrange
            var rule = MockPronunciationRuleRepository.GetSampleRule();
            rule.Description = null; // null description
            rule.Content     = null; // null content
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetPronunciationRuleByIdQuery(rule.PronunciationRuleId), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Description.Should().BeEmpty();
            result.Data!.Content.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-04",
                Description       = "Entity Description and Content are null → DTO maps to empty string via null-coalescing",
                ExpectedResult    = "DTO.Description='' and DTO.Content=''",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entity.Description = null", "entity.Content = null", "DTO uses ?? ''" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-05 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationRuleRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new GetPronunciationRuleByIdQuery("RULE-001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-05",
                Description       = "Repository throws exception → exception propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PR-GBI-06 | B | GetByIdAsync called with exact ruleId passed in query
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldCallGetByIdAsyncWithCorrectId()
        {
            // Arrange
            const string ruleId = "RULE-SPECIFIC";
            var repo = MockPronunciationRuleRepository.GetMock(
                getByIdResult: MockPronunciationRuleRepository.GetSampleRule(ruleId));
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(new GetPronunciationRuleByIdQuery(ruleId), CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetByIdAsync(ruleId), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetPronunciationRuleById",
                TestCaseID        = "TC-PR-GBI-06",
                Description       = "Boundary: verify GetByIdAsync is called exactly once with the exact PronunciationRuleId from query",
                ExpectedResult    = "GetByIdAsync('RULE-SPECIFIC') called Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific ruleId in query", "GetByIdAsync called once with that ID" }
            });
        }
    }
}
