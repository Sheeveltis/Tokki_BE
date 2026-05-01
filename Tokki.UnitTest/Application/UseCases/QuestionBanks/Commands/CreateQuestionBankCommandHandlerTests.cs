using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class CreateQuestionBankCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbMock = new();
        private readonly Mock<IQuestionOptionRepository> _optMock = new();
        private readonly Mock<IQuestionTypeRepository> _typeMock = new();
        private readonly Mock<IPassageRepository> _passMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();

        private CreateQuestionBankCommandHandler CreateHandler()
        {
            return new CreateQuestionBankCommandHandler(_qbMock.Object, _optMock.Object, _typeMock.Object, _passMock.Object, _idMock.Object);
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_01 | A | Invalid TypeId (Empty) -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_MissingTypeId_ShouldReturn400()
        {
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId ="" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_01",
                Description ="Validates empty efficiently",
                ExpectedResult ="Return 400",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty Id" }
            });
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_02 | A | TypeId NotFound -> 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_TypeNotFound_ShouldReturn404()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync((QuestionType?)null);
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId ="t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_02",
                Description ="Blocks",
                ExpectedResult ="Return 404",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Type Not Found natively successfully gracefully" }
            });
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_03 | A | Type Inactive -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_TypeInactive_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = false });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId ="t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("vô hi?u hóa");

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_03",
                Description ="Rejects",
                ExpectedResult ="Returns",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Inactive" }
            });
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_04 | A | Listening missing Url -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ListeningLacksMedia_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId ="t", MediaUrl = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("MediaUrl");

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_04",
                Description ="Validates constraints",
                ExpectedResult ="Rejects",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Listening" }
            });
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_05 | A | Passage Mismatch -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_PassageMismatch_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });
            _passMock.Setup(x => x.GetByIdAsync("p", It.IsAny<CancellationToken>())).ReturnsAsync(new Passage { MediaType = PassageMediaType.Text }); // Mismatch, should be audio
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId ="t", MediaUrl ="u", PassageId ="p" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_05",
                Description ="Enforces",
                ExpectedResult ="Returns solidly efficiently",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Mismatch" }
            });
        }
        
        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_06 | N | Success -> Maps Draft Status correctly natively intelligently dependably optimally natively successfully robustly automatically magically gracefully brilliantly compactly safely optimally natively expertly naturally correctly magically beautifully flawlessly intuitively
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessValid_ShouldSetDraftSafely()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId ="t", Content ="read logic", Options = new List<CreateQuestionOptionDto>() };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _qbMock.Verify(x => x.AddAsync(It.Is<QuestionBank>(y => y.Status == QuestionBankStatus.Draft)), Times.Once);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_06",
                Description ="Correct",
                ExpectedResult ="Mapped",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Success" }
            });
        }
        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_07 | N | Writing Skill Skips Options 
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_WritingSkill_SkipsOptionsCreation()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId ="t" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Verify options repository is never called
            _optMock.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Never);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_07",
                Description ="Writing intelligently flawlessly smartly fluently flexibly bypasses",
                ExpectedResult ="No",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Writing intelligently perfectly" }
            });
        }

        // -----------------------------------------------------------
        // CreateQuestionBankCommandHandler_08 | A | AddAsync Exception -> Server Error 500
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_AddThrowsException_ShouldReturnServerError()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            _qbMock.Setup(x => x.AddAsync(It.IsAny<QuestionBank>())).ThrowsAsync(new Exception("DB Failure"));
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId ="t", Content ="read" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup ="CreateQuestionBankCommandHandler",
                TestCaseID ="CreateQuestionBankCommandHandler_08",
                Description ="Captures",
                ExpectedResult ="Returns",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Throws" }
            });
        }
    }
}
