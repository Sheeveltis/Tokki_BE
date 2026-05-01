using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbRepoMock = new();
        private readonly Mock<IQuestionOptionRepository> _qoRepoMock = new();
        private readonly Mock<IQuestionTypeRepository> _qtRepoMock = new();
        private readonly Mock<IPassageRepository> _psgRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();

        public UpdateQuestionBankCommandHandlerTests()
        {
            _idGenMock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("rand-id");
        }

        private UpdateQuestionBankCommandHandler CreateHandler()
            => new(_qbRepoMock.Object, _qoRepoMock.Object, _qtRepoMock.Object, _psgRepoMock.Object, _idGenMock.Object);

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_01 | A | Empty ID -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_EmptyId_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_01",
                Description ="Returns error if QuestionBankId is empty",
                ExpectedResult ="Return 400 ValidationFailed",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Whitespace ID" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_02 | A | Not Found -> 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NotFound_ShouldReturn404()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1" };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((QuestionBank?)null);
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_02",
                Description ="Returns error if bank does not exist",
                ExpectedResult ="Return 404 QuestionBankNotFound",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"DB returns null" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_03 | A | Assigned Status -> 403
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_StatusAssigned_ShouldReturn403()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Assigned };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors[0].Description.Should().Contain("tr?ng thái Assigned");

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_03",
                Description ="Cannot update assigned questions",
                ExpectedResult ="Return 403 Forbidden",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Status Assigned" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_04 | A | Deleted Status -> 403
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_StatusDeleted_ShouldReturn403()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Deleted };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_04",
                Description ="Cannot update deleted questions",
                ExpectedResult ="Return 403 Forbidden",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Status Deleted" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_05 | A | Question Type Missing -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NullQuestionTypeId_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1", QuestionTypeId ="" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Draft, QuestionTypeId ="" };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_05",
                Description ="Rejects if final QuestionTypeId is empty",
                ExpectedResult ="Return 400",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"finalQuestionTypeId is empty" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_06 | N | Success -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_Success_ShouldReturn200()
        {
            var command = new UpdateQuestionBankCommand 
            { 
                QuestionBankId ="qb-1", 
                Content ="Updated text", 
                Options = new List<CreateQuestionOptionDto> 
                {
                    new CreateQuestionOptionDto { KeyOption ="1", Content ="Ans 1", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption ="2", Content ="Ans 2", IsCorrect = false }
                }
            };
            var qb = new QuestionBank { QuestionBankId ="qb-1", Status = QuestionBankStatus.Active, QuestionTypeId ="qt-1" };
            var qt = new QuestionType { IsActive = true, Skill = QuestionSkill.Reading };

            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _qtRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _qbRepoMock.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Once);
            _qoRepoMock.Verify(x => x.DeleteByQuestionBankIdAsync("qb-1", It.IsAny<CancellationToken>()), Times.Once);
            _qoRepoMock.Verify(x => x.AddRangeAsync(It.Is<List<QuestionOption>>(o => o.Count == 2)), Times.Once);
            _qbRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_06",
                Description ="Success updating reading question with 2 options",
                ExpectedResult ="Return 200",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Valid update" }
            });
        }
        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_07 | A | New QuestionType is Inactive -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_QuestionTypeInactive_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1", QuestionTypeId ="qt-1" };
            var qb = new QuestionBank { QuestionBankId ="qb-1", Status = QuestionBankStatus.Active, QuestionTypeId ="qt-1" };
            var qt = new QuestionType { IsActive = false, Skill = QuestionSkill.Reading };

            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _qtRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_07",
                Description ="Inactive",
                ExpectedResult ="Return 400",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Inactive" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionBankCommandHandler_08 | A | Exception -> 500
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_UpdateException_ShouldReturn500()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId ="qb-1", Content ="New" };
            var qb = new QuestionBank { QuestionBankId ="qb-1", Status = QuestionBankStatus.Active, QuestionTypeId ="qt-1" };
            var qt = new QuestionType { IsActive = true, Skill = QuestionSkill.Reading };

            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _qtRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            
            _qbRepoMock.Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>())).Throws(new Exception("DB Exception"));
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup ="UpdateQuestionBankCommandHandler",
                TestCaseID ="UpdateQuestionBankCommandHandler_08",
                Description ="Exception",
                ExpectedResult ="Return 500",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"DB" }
            });
        }
    }
}
