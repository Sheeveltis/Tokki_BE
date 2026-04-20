using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionTypes.Commands.DeleteQuestionType;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes
{
    public class DeleteQuestionTypeCommandHandlerTests
    {
        private static DeleteQuestionTypeCommandHandler CreateHandler(
            Mock<IQuestionTypeRepository>? repo = null)
        {
            return new DeleteQuestionTypeCommandHandler(
                (repo ?? MockQuestionTypeRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_01 | A | QuestionType not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotFound_ShouldReturn404()
        {
            // Arrange
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand("QT-MISSING");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_01",
                Description       = "QuestionType not found → 404 Failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_02 | N | Happy path: Active type → soft deleted, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveType_ShouldSoftDeleteAndReturn200()
        {
            // Arrange
            var qt      = MockQuestionTypeRepository.GetSampleActive();
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand(qt.QuestionTypeId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            qt.IsActive.Should().BeFalse();
            repo.Verify(x => x.UpdateAsync(It.IsAny<QuestionType>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_02",
                Description       = "Active QuestionType soft-deleted (IsActive=false), UpdateAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, entity.IsActive=false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionType found, IsActive=true", "set IsActive=false", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_03 | N | Already inactive → still returns 200 (idempotent)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyInactiveType_ShouldStillReturn200()
        {
            // Arrange
            var qt      = MockQuestionTypeRepository.GetSampleInactive();
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand(qt.QuestionTypeId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            qt.IsActive.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_03",
                Description       = "Already inactive QuestionType → 200 returned (idempotent soft delete)",
                ExpectedResult    = "IsSuccess=true, StatusCode=200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionType.IsActive already false", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_04 | A | Repository throws on UpdateAsync → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrowsOnUpdate_ShouldReturn500()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive();
            var repo = new Mock<IQuestionTypeRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);
            repo.Setup(x => x.UpdateAsync(It.IsAny<QuestionType>()))
                .ThrowsAsync(new InvalidOperationException("DB write error"));
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand(qt.QuestionTypeId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_04",
                Description       = "UpdateAsync throws → caught in try/catch → 500 returned",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateAsync throws", "catch block returns 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_05 | B | UpdateAsync called with entity having IsActive=false
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidDelete_UpdateAsyncCalledWithIsActiveFalse()
        {
            // Arrange
            var qt      = MockQuestionTypeRepository.GetSampleActive();
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand(qt.QuestionTypeId);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.UpdateAsync(It.Is<QuestionType>(q => q.IsActive == false)), Times.Once);

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_05",
                Description       = "Boundary: UpdateAsync called with entity.IsActive=false",
                ExpectedResult    = "UpdateAsync(entity where IsActive=false) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entity.IsActive set to false before UpdateAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteQuestionType_06 | B | GetByIdAsync called with exactly the correct Id
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidDelete_GetByIdCalledWithCorrectId()
        {
            // Arrange
            const string qtId = "QT-SPECIFIC-01";
            var qt      = MockQuestionTypeRepository.GetSampleActive(qtId);
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var command = new DeleteQuestionTypeCommand(qtId);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetByIdAsync(qtId, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionType",
                TestCaseID        = "DeleteQuestionType_06",
                Description       = "Boundary: GetByIdAsync called once with the exact QuestionTypeId from command",
                ExpectedResult    = "GetByIdAsync('QT-SPECIFIC-01') Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific id in command", "repo called with that id exactly" }
            });
        }
    }
}
