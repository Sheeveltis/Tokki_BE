using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionTypes.Commands.CreateQuestionType;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes
{
    public class CreateQuestionTypeCommandHandlerTests
    {
        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "QT-GEN-001")
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return mock;
        }

        private static CreateQuestionTypeCommandHandler CreateHandler(
            Mock<IQuestionTypeRepository>? repo  = null,
            Mock<IIdGeneratorService>?     idGen = null)
        {
            return new CreateQuestionTypeCommandHandler(
                (repo  ?? MockQuestionTypeRepository.GetMock()).Object,
                (idGen ?? GetIdGenMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-01 | A | Duplicate name → 400 Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateName_ShouldReturnFailure()
        {
            // Arrange — name already exists
            var repo    = MockQuestionTypeRepository.GetMock(nameExists: true);
            var handler = CreateHandler(repo: repo);
            var command = new CreateQuestionTypeCommand { Name = "Reading Basic", Skill = QuestionSkill.Reading };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("tên");

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-01",
                Description       = "Duplicate Name → IsSuccess=false with name-conflict message",
                ExpectedResult    = "IsSuccess=false, Message contains 'tên'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync=true", "failure returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-02 | A | Duplicate code → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateCode_ShouldReturnFailure()
        {
            // Arrange — name ok, code already exists
            var repo    = MockQuestionTypeRepository.GetMock(nameExists: false, codeExists: true);
            var handler = CreateHandler(repo: repo);
            var command = new CreateQuestionTypeCommand { Name = "Unique Name", Code = "DUP001", Skill = QuestionSkill.Reading };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("code");

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-02",
                Description       = "Duplicate Code → failure with code-conflict message",
                ExpectedResult    = "IsSuccess=false, Message contains 'code'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCodeExistsAsync=true", "failure returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-03 | N | Happy path → created, 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_ShouldReturn201WithGeneratedId()
        {
            // Arrange
            var repo    = MockQuestionTypeRepository.GetMock(nameExists: false, codeExists: false);
            var idGen   = GetIdGenMock("QT-GEN-001");
            var handler = CreateHandler(repo: repo, idGen: idGen);
            var command = new CreateQuestionTypeCommand
            {
                Name        = "New Type",
                Code        = "NT001",
                Skill       = QuestionSkill.Reading,
                Description = "A new question type"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("QT-GEN-001");
            repo.Verify(x => x.AddAsync(It.Is<QuestionType>(qt => qt.IsActive == true && qt.Name == "New Type")), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-03",
                Description       = "Happy path: valid command → QuestionType created as IsActive=true, 201 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=201, Data='QT-GEN-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Name unique, Code unique", "AddAsync+SaveChanges called", "201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-04 | N | Null Code → code-exists check skipped, 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullCode_ShouldSkipCodeExistsCheck()
        {
            // Arrange — codeExists=true but Code is null → should skip
            var repo  = MockQuestionTypeRepository.GetMock(nameExists: false, codeExists: true);
            var handler = CreateHandler(repo: repo);
            var command = new CreateQuestionTypeCommand
            {
                Name  = "No Code Type",
                Code  = null, // null → skips IsCodeExistsAsync check
                Skill = QuestionSkill.Writing
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert — should succeed because Code is null
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-04",
                Description       = "Code=null → IsCodeExistsAsync never called, create succeeds",
                ExpectedResult    = "IsSuccess=true, IsCodeExistsAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Code=null", "code-exists check skipped", "success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-05 | B | Generated ID assigned to entity
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_GeneratedIdAssignedToEntity()
        {
            // Arrange
            const string expectedId = "QT-FIXED-ID";
            var idGen   = GetIdGenMock(expectedId);
            var repo    = MockQuestionTypeRepository.GetMock();
            var handler = CreateHandler(repo: repo, idGen: idGen);
            var command = new CreateQuestionTypeCommand { Name = "Fixed", Skill = QuestionSkill.Listening };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Data.Should().Be(expectedId);
            repo.Verify(x => x.AddAsync(It.Is<QuestionType>(qt => qt.QuestionTypeId == expectedId)), Times.Once);

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-05",
                Description       = "Boundary: IdGenerator value is assigned as QuestionTypeId on entity",
                ExpectedResult    = "Data='QT-FIXED-ID', entity.QuestionTypeId='QT-FIXED-ID'",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IdGenerator returns fixed id", "assigned to entity" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-CRE-06 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IQuestionTypeRepository>();
            repo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<QuestionType>())).ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(repo: repo);
            var command = new CreateQuestionTypeCommand { Name = "Good Name", Skill = QuestionSkill.Reading };

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            QACollector.LogTestCase("Question Type - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionType",
                TestCaseID        = "TC-QT-CRE-06",
                Description       = "Repository AddAsync throws → exception propagates (no catch in handler)",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws", "no catch block in handler" }
            });
        }
    }
}
