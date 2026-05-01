using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.Commands.DeletePronunciationRule;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class DeletePronunciationRuleCommandHandlerTests
    {
        private static DeletePronunciationRuleCommandHandler CreateHandler(
            Mock<IPronunciationRuleRepository>? repo = null)
        {
            return new DeletePronunciationRuleCommandHandler(
                (repo ?? MockPronunciationRuleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_01 | A | RuleId not found → 404 Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleNotFound_ShouldReturn404Failure()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(repo);
            var command = new DeletePronunciationRuleCommand("RULE-NOTEXIST");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_01",
                Description       = "PronunciationRuleId does not exist → return 404 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 Failure returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_02 | N | Happy path → returns true, StatusCode=200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleFound_ShouldReturnTrueWith200()
        {
            // Arrange
            var rule = MockPronunciationRuleRepository.GetSampleRule();
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var handler = CreateHandler(repo);
            var command = new DeletePronunciationRuleCommand(rule.PronunciationRuleId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_02",
                Description       = "Happy path: rule found → deleted successfully, returns true with 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data=true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns entity", "DeleteAsync+SaveChanges called", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_03 | N | DeleteAsync and SaveChangesAsync each called once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleFound_ShouldCallDeleteAndSaveOnce()
        {
            // Arrange
            var rule = MockPronunciationRuleRepository.GetSampleRule();
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(
                new DeletePronunciationRuleCommand(rule.PronunciationRuleId), CancellationToken.None);

            // Assert
            repo.Verify(x => x.DeleteAsync(It.IsAny<Domain.Entities.PronunciationRule>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_03",
                Description       = "Rule found → DeleteAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "DeleteAsync Times.Once, SaveChangesAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Entity found", "DeleteAsync once, SaveChangesAsync once" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_04 | A | Repository GetByIdAsync throws → propagates
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
                new DeletePronunciationRuleCommand("RULE-001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_04",
                Description       = "Repository throws exception on GetByIdAsync → exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_05 | A | Rule not found → DeleteAsync never called
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RuleNotFound_ShouldNotCallDeleteAsync()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(
                new DeletePronunciationRuleCommand("RULE-NOTEXIST"), CancellationToken.None);

            // Assert
            repo.Verify(x => x.DeleteAsync(It.IsAny<Domain.Entities.PronunciationRule>()), Times.Never);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_05",
                Description       = "Rule not found → DeleteAsync and SaveChangesAsync never called",
                ExpectedResult    = "DeleteAsync Times.Never, SaveChangesAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "early return → no delete" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeletePronunciationRule_06 | B | Empty ruleId → not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyRuleId_ShouldReturn404()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(repo);
            var command = new DeletePronunciationRuleCommand(string.Empty);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            repo.Verify(x => x.GetByIdAsync(string.Empty), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeletePronunciationRule",
                TestCaseID        = "DeletePronunciationRule_06",
                Description       = "Boundary: empty PronunciationRuleId → GetByIdAsync returns null → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404, GetByIdAsync(empty) called once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PronunciationRuleId = string.Empty", "GetByIdAsync returns null" }
            });
        }
    }
}
