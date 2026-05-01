using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationExample
{
    public class GetExamplesByRuleIdQueryHandlerTests
    {
        private static GetExamplesByRuleIdQueryHandler CreateHandler(
            Mock<IPronunciationExampleRepository>? repo = null)
        {
            return new GetExamplesByRuleIdQueryHandler(
                (repo ?? MockPronunciationExampleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_01 | N | No examples for ruleId → empty list, IsSuccess=true
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoExamplesForRule_ShouldReturnEmptyListSuccess()
        {
            // Arrange
            var repo = MockPronunciationExampleRepository.GetMock(
                getByRuleIdResult: new List<Tokki.Domain.Entities.PronunciationExample>());
            var handler = CreateHandler(repo);
            var query = new GetExamplesByRuleIdQuery("RULE-EMPTY");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_01",
                Description       = "No examples exist for the given ruleId → returns empty list with IsSuccess=true",
                ExpectedResult    = "IsSuccess=true, Data is empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetExamplesByRuleIdAsync returns [] ", "select maps empty list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_02 | N | Happy path → StatusCode=200, mapped list returned
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRuleWithExamples_ShouldReturn200()
        {
            // Arrange
            var examples = MockPronunciationExampleRepository.GetSampleExampleList("RULE-001");
            var repo = MockPronunciationExampleRepository.GetMock(getByRuleIdResult: examples);
            var handler = CreateHandler(repo);
            var query = new GetExamplesByRuleIdQuery("RULE-001");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(examples.Count);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_02",
                Description       = "Happy path: valid ruleId with examples → Returns 200 with mapped list",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, list count matches",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid ruleId", "3 examples returned by repo", "StatusCode=200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_03 | N | Multiple examples → all mapped to ExampleSimpleDTO correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MultipleExamples_ShouldMapAllDTOsCorrectly()
        {
            // Arrange
            var examples = MockPronunciationExampleRepository.GetSampleExampleList("RULE-001");
            var repo = MockPronunciationExampleRepository.GetMock(getByRuleIdResult: examples);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetExamplesByRuleIdQuery("RULE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            for (int i = 0; i < examples.Count; i++)
            {
                result.Data![i].ExampleId.Should().Be(examples[i].ExampleId);
                result.Data![i].RawScript.Should().Be(examples[i].RawScript);
                result.Data![i].SortOrder.Should().Be(examples[i].SortOrder);
            }

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_03",
                Description       = "Multiple examples returned → all mapped to ExampleSimpleDTO (ExampleId, RawScript, SortOrder)",
                ExpectedResult    = "All DTO properties match source entity",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "3 entities in list", "Each DTO field matched" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_04 | A | Repository throws exception → propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationExampleRepository>();
            repo.Setup(x => x.GetExamplesByRuleIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB connection failed"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new GetExamplesByRuleIdQuery("RULE-001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("DB connection failed");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_04",
                Description       = "Repository throws InvalidOperationException → exception propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetExamplesByRuleIdAsync throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_05 | B | Empty ruleId → returns empty list (no filter applied in handler)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyRuleId_ShouldReturnEmptyList()
        {
            // Arrange
            var repo = MockPronunciationExampleRepository.GetMock(
                getByRuleIdResult: new List<Tokki.Domain.Entities.PronunciationExample>());
            var handler = CreateHandler(repo);
            var query = new GetExamplesByRuleIdQuery(string.Empty);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            repo.Verify(x => x.GetExamplesByRuleIdAsync(
                string.Empty, It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_05",
                Description       = "Boundary: empty ruleId passed through to repo → returns empty list",
                ExpectedResult    = "IsSuccess=true, Data=[]. GetExamplesByRuleIdAsync called with empty string",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PronunciationRuleId = string.Empty", "repo returns empty list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetExamplesByRuleId_06 | N | GetExamplesByRuleIdAsync called with correct ruleId
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRuleId_ShouldCallRepoWithCorrectRuleId()
        {
            // Arrange
            const string ruleId = "RULE-SPECIFIC-01";
            var repo = MockPronunciationExampleRepository.GetMock(
                getByRuleIdResult: new List<Tokki.Domain.Entities.PronunciationExample>());
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(new GetExamplesByRuleIdQuery(ruleId), CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetExamplesByRuleIdAsync(
                ruleId, It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get By Rule", new TestCaseDetail
            {
                FunctionGroup     = "GetExamplesByRuleId",
                TestCaseID        = "GetExamplesByRuleId_06",
                Description       = "Verify GetExamplesByRuleIdAsync is called exactly once with the correct ruleId",
                ExpectedResult    = "GetExamplesByRuleIdAsync(ruleId) called Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific ruleId provided", "Repo called once with exact ruleId" }
            });
        }
    }
}
