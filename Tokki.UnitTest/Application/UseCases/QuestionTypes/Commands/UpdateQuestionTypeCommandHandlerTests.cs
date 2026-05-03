using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType
{
    public class UpdateQuestionTypeCommandHandlerTests
    {
        private readonly Mock<IQuestionTypeRepository> _repoMock = new();

        private UpdateQuestionTypeCommandHandler CreateHandler() => new(_repoMock.Object);

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_01 | A | NotFound -> Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NotFound_ShouldReturnFailure()
        {
            var command = new UpdateQuestionTypeCommand { QuestionTypeId = "qt-1" };
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((QuestionType?)null);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm th?y");

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_01",
                Description = "Returns error if question type is not found",
                ExpectedResult = "Return generic failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_02 | A | NameExists -> Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NameExists_ShouldReturnFailure()
        {
            var command = new UpdateQuestionTypeCommand { QuestionTypeId = "qt-1", Name = "New Name" };
            var qt = new QuestionType { QuestionTypeId = "qt-1", Name = "Old Name" };
            
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            _repoMock.Setup(x => x.IsNameExistsAsync("New Name", "qt-1")).ReturnsAsync(true);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Tên lo?i câu h?i dã t?n t?i");

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_02",
                Description = "Returns error if new name is duplicated",
                ExpectedResult = "Return generic failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync returns true" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_03 | A | CodeExists -> Failure
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_CodeExists_ShouldReturnFailure()
        {
            var command = new UpdateQuestionTypeCommand { QuestionTypeId = "qt-1", Code = "New Code" };
            var qt = new QuestionType { QuestionTypeId = "qt-1", Code = "Old Code" };
            
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            _repoMock.Setup(x => x.IsCodeExistsAsync("New Code", "qt-1")).ReturnsAsync(true);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Mã code dã t?n t?i");

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_03",
                Description = "Returns error if new code is duplicated",
                ExpectedResult = "Return generic failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCodeExistsAsync returns true" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_04 | N | Swallows"string" updates -> Partial Update
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_IgnoreStringPlaceholders_ShouldPartialUpdate()
        {
            var command = new UpdateQuestionTypeCommand 
            { 
                QuestionTypeId = "qt-1", 
                Name = "string", // Swagger placeholder, should be ignored
                Code = "string",
                Description = "string",
                Status = 1
            };
            var qt = new QuestionType { QuestionTypeId = "qt-1", Name = "Real", Code = "OLD", Description = "Desc" };
            
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            qt.Name.Should().Be("Real"); // Not modified
            qt.Code.Should().Be("OLD"); // Not modified
            qt.Description.Should().Be("Desc"); // Not modified
            qt.IsActive.Should().BeTrue();

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_04",
                Description = "Ignores 'string' placeholders from Swagger defaults",
                ExpectedResult = "Return 200, fields unmodified",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Fields contain 'string'" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_05 | N | Change Only Enums -> Success
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ChangeEnums_ShouldUpdateEnumsCorrectly()
        {
            var command = new UpdateQuestionTypeCommand 
            { 
                QuestionTypeId = "qt-1", 
                Skill = QuestionSkill.Reading,
                Difficulty = DifficultyLevel.Medium,
                ExamType = ExamType.TopikI,
                Status = 0
            };
            var qt = new QuestionType { QuestionTypeId = "qt-1", Skill = QuestionSkill.Listening, Difficulty = DifficultyLevel.Easy, ExamType = ExamType.TopikII, IsActive = true };
            
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            qt.Skill.Should().Be(QuestionSkill.Reading);
            qt.Difficulty.Should().Be(DifficultyLevel.Medium);
            qt.ExamType.Should().Be(ExamType.TopikI);
            qt.IsActive.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_05",
                Description = "Updates enums and status correctly",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid enum updates" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionTypeCommandHandler_06 | N | Full Update -> Success
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_FullUpdate_ShouldUpdateEverythingAndSave()
        {
            var command = new UpdateQuestionTypeCommand 
            { 
                QuestionTypeId = "qt-1", 
                Name = "New Name",
                Code = "N-1",
                Description = "Desc-1",
                Status = 1
            };
            var qt = new QuestionType { QuestionTypeId = "qt-1", Name = "X", Code = "Y", Description = "Z", IsActive = false };
            
            _repoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            _repoMock.Setup(x => x.IsNameExistsAsync("New Name", "qt-1")).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsCodeExistsAsync("N-1", "qt-1")).ReturnsAsync(false);
                     
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            qt.Name.Should().Be("New Name");
            qt.Code.Should().Be("N-1");
            qt.Description.Should().Be("Desc-1");

            _repoMock.Verify(x => x.UpdateAsync(qt), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionTypeCommandHandler",
                TestCaseID = "UpdateQuestionTypeCommandHandler_06",
                Description = "Full proper update saves to DB successfully",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid full update fields" }
            });
        }
    }
}
