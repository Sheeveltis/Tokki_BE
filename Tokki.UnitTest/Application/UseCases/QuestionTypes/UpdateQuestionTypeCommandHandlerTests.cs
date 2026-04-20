using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes
{
    public class UpdateQuestionTypeCommandHandlerTests
    {
        private static UpdateQuestionTypeCommandHandler CreateHandler(
            Mock<IQuestionTypeRepository>? repo = null)
        {
            return new UpdateQuestionTypeCommandHandler(
                (repo ?? MockQuestionTypeRepository.GetMock()).Object);
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_01 | A | QuestionType not found ? failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NotFound_ShouldReturnFailure()
        {
            // Arrange
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand { QuestionTypeId = "QT-MISSING", Status = 1 };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_01",
                Description       = "QuestionType not found ? failure (no status code specified in handler, defaults)",
                ExpectedResult    = "IsSuccess=false",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "failure returned" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_02 | A | Duplicate name (different entity) ? failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateName_ShouldReturnFailure()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive("QT-001");
            var repo = MockQuestionTypeRepository.GetMock(returnedById: qt, nameExists: true);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand
            {
                QuestionTypeId = "QT-001",
                Name           = "Existing Name", // differs from current"Reading Basic" ? triggers check
                Status         = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("tên");

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_02",
                Description       = "New name already taken by another entity ? failure with name-conflict message",
                ExpectedResult    = "IsSuccess=false, Message contains 'tên'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Name changed to existing name", "IsNameExistsAsync=true", "failure" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_03 | A | Duplicate code ? failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateCode_ShouldReturnFailure()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive("QT-001");
            var repo = MockQuestionTypeRepository.GetMock(returnedById: qt, codeExists: true);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand
            {
                QuestionTypeId = "QT-001",
                Name           = "Reading Basic",   // same name ? skips name check
                Code           = "CONFLICT001",     // differs from current"RB001" ? triggers check
                Status         = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("code");

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_03",
                Description       = "Code conflicts with another entity ? failure with code-conflict message",
                ExpectedResult    = "IsSuccess=false, Message contains 'code'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Code changed, IsCodeExistsAsync=true", "failure" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_04 | N | Happy path: patch name + description, 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidUpdate_ShouldReturn200()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive("QT-001");
            var repo = MockQuestionTypeRepository.GetMock(returnedById: qt, nameExists: false, codeExists: false);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand
            {
                QuestionTypeId = "QT-001",
                Name           = "Updated Name",
                Description    = "Updated description",
                Status         = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            qt.Name.Should().Be("Updated Name");
            qt.Description.Should().Be("Updated description");
            repo.Verify(x => x.UpdateAsync(It.IsAny<QuestionType>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_04",
                Description       = "Happy path: name and description updated, UpdateAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, entity updated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid name, no conflicts", "200 returned" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_05 | N | Status=1 ? IsActive=true; Status=0 ? IsActive=false
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_Status0_ShouldSetIsActiveFalse()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive("QT-001");
            var repo = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand
            {
                QuestionTypeId = "QT-001",
                Name           = "Reading Basic", // same ? no name check
                Status         = 0               // deactivate
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            qt.IsActive.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_05",
                Description       = "Status=0 ? entity.IsActive set to false",
                ExpectedResult    = "IsSuccess=true, entity.IsActive=false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=0", "IsActive=false applied" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionType_06 | N | Same name as current ? name-check skipped
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SameNameAsCurrent_ShouldSkipNameCheck()
        {
            // Arrange
            var qt   = MockQuestionTypeRepository.GetSampleActive("QT-001"); // Name = "Reading Basic"
            var repo = MockQuestionTypeRepository.GetMock(returnedById: qt, nameExists: true);
            var handler = CreateHandler(repo);
            var command = new UpdateQuestionTypeCommand
            {
                QuestionTypeId = "QT-001",
                Name           = "Reading Basic", // same as entity.Name ? skips IsNameExistsAsync
                Status         = 1
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionType",
                TestCaseID        = "UpdateQuestionType_06",
                Description       = "Name unchanged (same as current entity) ? IsNameExistsAsync never called",
                ExpectedResult    = "IsSuccess=true, IsNameExistsAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Name == entity.Name", "name-check skipped", "success" }
            });
        }
    }
}
