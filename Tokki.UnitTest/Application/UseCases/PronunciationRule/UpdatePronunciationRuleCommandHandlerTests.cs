using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.Commands.UpdatePronunciationRule;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class UpdatePronunciationRuleCommandHandlerTests
    {
        private static UpdatePronunciationRuleCommandHandler CreateHandler(
            Mock<IPronunciationRuleRepository>? repo = null)
        {
            return new UpdatePronunciationRuleCommandHandler(
                (repo ?? MockPronunciationRuleRepository.GetMock()).Object);
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_01 | A | RuleId not found ? 404 Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RuleNotFound_ShouldReturn404Failure()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = "RULE-NOTEXIST",
                RuleName            = "New Name",
                SortOrder           = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_01",
                Description       = "PronunciationRuleId does not exist ? return 404 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 Failure returned" }
            });
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_02 | A | RuleName duplicate (excluding self) ? 400 Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateRuleNameExcludingSelf_ShouldReturn400Failure()
        {
            // Arrange
            var existingRule = MockPronunciationRuleRepository.GetSampleRule("RULE-001", "?? ??");
            var repo = MockPronunciationRuleRepository.GetMock(
                getByIdResult: existingRule,
                ruleNameExists: true);          // another rule has the same name
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = "RULE-001",
                RuleName            = "?? ??", // name already taken by another rule
                SortOrder           = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_02",
                Description       = "RuleName already taken by another rule (excluding self) ? return 400 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsRuleNameExistsAsync(name, excludeId) returns true", "400 Failure returned" }
            });
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_03 | N | Happy path ? returns true, StatusCode=200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidUpdate_ShouldReturnTrueWith200()
        {
            // Arrange
            var existingRule = MockPronunciationRuleRepository.GetSampleRule();
            var repo = MockPronunciationRuleRepository.GetMock(
                getByIdResult: existingRule,
                ruleNameExists: false);
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = existingRule.PronunciationRuleId,
                RuleName            = "Updated Rule Name",
                Description         = "Updated description",
                Content             = "Updated content",
                SortOrder           = 5,
                UpdateBy            = "ADMIN-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_03",
                Description       = "Happy path: rule found and new name unique ? updates and returns true with 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data=true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns entity", "IsRuleNameExistsAsync=false", "UpdateAsync+SaveChanges called" }
            });
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_04 | N | UpdateAsync and SaveChangesAsync each called exactly once
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidUpdate_ShouldCallUpdateAndSaveOnce()
        {
            // Arrange
            var existingRule = MockPronunciationRuleRepository.GetSampleRule();
            var repo = MockPronunciationRuleRepository.GetMock(
                getByIdResult: existingRule,
                ruleNameExists: false);
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = existingRule.PronunciationRuleId,
                RuleName            = "Unique New Name",
                SortOrder           = 2
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.PronunciationRule>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_04",
                Description       = "Happy path ? UpdateAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateAsync Times.Once, SaveChangesAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Entity found", "Name unique", "UpdateAsync once, SaveChanges once" }
            });
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_05 | A | Repository throws on GetByIdAsync ? exception propagates
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationRuleRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = "RULE-001",
                RuleName            = "Test",
                SortOrder           = 1
            };

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_05",
                Description       = "Repository throws exception on GetByIdAsync ? exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws exception" }
            });
        }

        // -----------------------------------------------------------
        // UpdatePronunciationRule_06 | B | RuleName with spaces ? trimmed before IsRuleNameExistsAsync
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RuleNameWithSpaces_ShouldTrimBeforeDuplicateCheck()
        {
            // Arrange
            var existingRule = MockPronunciationRuleRepository.GetSampleRule("RULE-001");
            var repo = MockPronunciationRuleRepository.GetMock(
                getByIdResult: existingRule,
                ruleNameExists: false);
            var handler = CreateHandler(repo);
            var command = new UpdatePronunciationRuleCommand
            {
                PronunciationRuleId = "RULE-001",
                RuleName            = "  ???",
                SortOrder           = 1
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert: IsRuleNameExistsAsync called with trimmed name and excludeId
            repo.Verify(x => x.IsRuleNameExistsAsync("???", "RULE-001"), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdatePronunciationRule",
                TestCaseID        = "UpdatePronunciationRule_06",
                Description       = "Boundary: RuleName with leading/trailing spaces ? trimmed before IsRuleNameExistsAsync call with excludeId",
                ExpectedResult    = "IsRuleNameExistsAsync('???', 'RULE-001') called once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RuleName = '  ???  '", "Trim() applied", "excludeId = PronunciationRuleId" }
            });
        }
    }
}
