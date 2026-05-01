using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class CreatePronunciationRuleCommandHandlerTests
    {
        private static CreatePronunciationRuleCommandHandler CreateHandler(
            Mock<IPronunciationRuleRepository>? repo      = null,
            Mock<IIdGeneratorService>?          idGen     = null)
        {
            var mockIdGen = idGen ?? new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("RULE-NEW01");

            return new CreatePronunciationRuleCommandHandler(
                (repo ?? MockPronunciationRuleRepository.GetMock()).Object,
                mockIdGen.Object);
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_01 | A | RuleName already exists ? 400 Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateRuleName_ShouldReturn400Failure()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(ruleNameExists: true);
            var handler = CreateHandler(repo);
            var command = new CreatePronunciationRuleCommand
            {
                RuleName    = "?? ??",
                Description = "Test desc",
                CreateBy    = "ADMIN-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_01",
                Description       = "RuleName already exists in repository ? return 400 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsRuleNameExistsAsync returns true", "handler returns Failure 400" }
            });
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_02 | N | Happy path ? rule created, new ID returned, StatusCode=200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidCommand_ShouldReturnNewIdWith200()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(ruleNameExists: false);
            var idGen = new Mock<IIdGeneratorService>();
            idGen.Setup(x => x.GenerateCustom(10)).Returns("RULE-NEW01");
            var handler = CreateHandler(repo, idGen);
            var command = new CreatePronunciationRuleCommand
            {
                RuleName    = "??? ??",
                Description = "New rule description",
                Content     = "Content here",
                CreateBy    = "ADMIN-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("RULE-NEW01");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_02",
                Description       = "Happy path: valid unique RuleName ? rule created and new ID returned with 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data='RULE-NEW01'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsRuleNameExistsAsync=false", "GenerateCustom returns ID", "AddAsync+SaveChanges called" }
            });
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_03 | N | AddAsync and SaveChangesAsync each called exactly once
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidCommand_ShouldCallAddAndSaveOnce()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(ruleNameExists: false);
            var handler = CreateHandler(repo);
            var command = new CreatePronunciationRuleCommand
            {
                RuleName = "?? ??",
                CreateBy = "ADMIN-001"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.PronunciationRule>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_03",
                Description       = "Verify AddAsync and SaveChangesAsync each called exactly once on happy path",
                ExpectedResult    = "AddAsync Times.Once, SaveChangesAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique rule name", "AddAsync once, SaveChangesAsync once" }
            });
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_04 | A | Repository throws ? exception propagates
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IPronunciationRuleRepository>();
            repo.Setup(x => x.IsRuleNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(repo);
            var command = new CreatePronunciationRuleCommand { RuleName = "Test Rule" };

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_04",
                Description       = "Repository throws exception on IsRuleNameExistsAsync ? exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsRuleNameExistsAsync throws exception" }
            });
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_05 | B | RuleName with leading/trailing spaces ? trimmed before duplicate check
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RuleNameWithSpaces_ShouldTrimBeforeCheck()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(ruleNameExists: false);
            var handler = CreateHandler(repo);
            var command = new CreatePronunciationRuleCommand
            {
                RuleName = "  ???",
                CreateBy = "ADMIN-001"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert: IsRuleNameExistsAsync called with trimmed name
            repo.Verify(x => x.IsRuleNameExistsAsync("???", It.IsAny<string?>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_05",
                Description       = "Boundary: RuleName has leading/trailing spaces ? trimmed before IsRuleNameExistsAsync call",
                ExpectedResult    = "IsRuleNameExistsAsync called with '???' (trimmed)",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RuleName = '  ???  '", "Trim() applied before check" }
            });
        }

        // -----------------------------------------------------------
        // CreatePronunciationRule_06 | N | Duplicate check ? AddAsync never called on 400 path
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateRuleName_ShouldNotCallAddAsync()
        {
            // Arrange
            var repo = MockPronunciationRuleRepository.GetMock(ruleNameExists: true);
            var handler = CreateHandler(repo);
            var command = new CreatePronunciationRuleCommand { RuleName = "Existing Rule" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.PronunciationRule>()), Times.Never);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("Pronunciation Rule - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreatePronunciationRule",
                TestCaseID        = "CreatePronunciationRule_06",
                Description       = "Duplicate rule name ? AddAsync and SaveChangesAsync never called",
                ExpectedResult    = "AddAsync Times.Never, SaveChangesAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsRuleNameExistsAsync=true", "early return ? no persistence" }
            });
        }
    }
}
