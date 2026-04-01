using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationExample
{
    public class GetExampleDetailQueryHandlerTests
    {
        private static GetExampleDetailQueryHandler CreateHandler(
            Mock<IPronunciationExampleRepository>? repo = null)
        {
            return new GetExampleDetailQueryHandler(
                (repo ?? MockPronunciationExampleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-01 | A | ExampleId not found → NOT_FOUND failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturnNotFoundFailure()
        {
            // Arrange
            var repo = MockPronunciationExampleRepository.GetMock(getDetailByIdResult: null);
            var handler = CreateHandler(repo);
            var query = new GetExampleDetailQuery("EX-NOTEXIST");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-01",
                Description       = "ExampleId does not exist in repository → NOT_FOUND failure",
                ExpectedResult    = "IsSuccess = false, error NOT_FOUND",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetDetailByIdAsync returns null", "handler returns Failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-02 | A | Repository throws exception → propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationExampleRepository>();
            repo.Setup(x => x.GetDetailByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new GetExampleDetailQuery("EX-0001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-02",
                Description       = "Repository throws InvalidOperationException → exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetDetailByIdAsync throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-03 | N | Example found without nav prop → rule fields are empty strings
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleFoundWithoutRule_ShouldReturnEmptyRuleFields()
        {
            // Arrange
            var example = MockPronunciationExampleRepository.GetSampleExample();
            example.PronunciationRule = null; // no navigation property loaded
            var repo = MockPronunciationExampleRepository.GetMock(getDetailByIdResult: example);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetExampleDetailQuery(example.ExampleId), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.RuleName.Should().BeEmpty();
            result.Data!.RuleDescription.Should().BeEmpty();
            result.Data!.RuleContent.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-03",
                Description       = "Example found but PronunciationRule nav prop is null → DTO rule fields empty",
                ExpectedResult    = "RuleName/Description/Content = empty string",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entity.PronunciationRule == null", "null-coalescing returns empty string" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-04 | N | Example found with PronunciationRule nav prop → DTO fully mapped
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleFoundWithRule_ShouldMapRuleFieldsCorrectly()
        {
            // Arrange
            var example = MockPronunciationExampleRepository.GetSampleExampleWithRule();
            var repo = MockPronunciationExampleRepository.GetMock(getDetailByIdResult: example);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetExampleDetailQuery(example.ExampleId), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.RuleName.Should().Be(example.PronunciationRule!.RuleName);
            result.Data!.RuleDescription.Should().Be(example.PronunciationRule!.Description);
            result.Data!.RuleContent.Should().Be(example.PronunciationRule!.Content);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-04",
                Description       = "Example found with PronunciationRule nav prop → DTO rule fields correctly mapped",
                ExpectedResult    = "RuleName/Description/Content match entity navigation property",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entity.PronunciationRule != null", "DTO fields mapped from nav prop" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-05 | N | Happy path → IsSuccess=true, StatusCode=200, data mapped
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExample_ShouldReturn200WithMappedDTO()
        {
            // Arrange
            var example = MockPronunciationExampleRepository.GetSampleExample();
            var repo = MockPronunciationExampleRepository.GetMock(getDetailByIdResult: example);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetExampleDetailQuery(example.ExampleId), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.ExampleId.Should().Be(example.ExampleId);
            result.Data!.RawScript.Should().Be(example.RawScript);
            result.Data!.PhoneticScript.Should().Be(example.PhoneticScript);
            result.Data!.Meaning.Should().Be(example.Meaning);
            result.Data!.AudioUrl.Should().Be(example.AudioUrl);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-05",
                Description       = "Happy path: valid ExampleId → Returns 200 with correctly mapped ExampleDetailDTO",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, all DTO fields mapped",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid ExampleId", "Repository returns entity", "DTO fully mapped" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-PE-GD-06 | B | ExampleId = empty string → not found → Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyExampleId_ShouldReturnFailure()
        {
            // Arrange
            var repo = MockPronunciationExampleRepository.GetMock(getDetailByIdResult: null);
            var handler = CreateHandler(repo);
            var query = new GetExampleDetailQuery(string.Empty);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            repo.Verify(x => x.GetDetailByIdAsync(
                string.Empty, It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Example - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetExampleDetail",
                TestCaseID        = "TC-PE-GD-06",
                Description       = "Boundary: ExampleId = empty string → repo returns null → Failure",
                ExpectedResult    = "IsSuccess=false, GetDetailByIdAsync called with empty string",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExampleId = string.Empty", "GetDetailByIdAsync returns null" }
            });
        }
    }
}
