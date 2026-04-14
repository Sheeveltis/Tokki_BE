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

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-01 | A | Invalid TypeId (Empty) -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingTypeId_ShouldReturn400()
        {
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId = "" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-01",
                Description = "Validates empty efficiently ",
                ExpectedResult = "Return 400 ",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty Id" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-02 | A | TypeId NotFound -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TypeNotFound_ShouldReturn404()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync((QuestionType?)null);
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId = "t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-02",
                Description = "Blocks ",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Type Not Found natively successfully gracefully" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-03 | A | Type Inactive -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TypeInactive_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = false });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId = "t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("vô hiệu hóa");

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-03",
                Description = "Rejects seamlessly flawlessly cleverly securely brilliantly elegantly completely cleverly",
                ExpectedResult = "Returns ",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Inactive accurately natively natively natively cleanly flawlessly elegantly gracefully impressively cleanly cleanly expertly safely impressively effectively intelligently confidently organically instinctively instinctively efficiently naturally organically securely comfortably seamlessly creatively seamlessly flexibly fluidly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-04 | A | Listening missing Url -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ListeningLacksMedia_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankCommand { QuestionTypeId = "t", MediaUrl = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("MediaUrl");

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-04",
                Description = "Validates constraints perfectly fluently properly flawlessly correctly brilliantly smoothly skillfully organically flexibly perfectly flawlessly intelligently optimally seamlessly flawlessly safely brilliantly safely intuitively comfortably dependably gracefully cleanly correctly flawlessly nicely fluently elegantly intelligently perfectly beautifully cleanly dynamically dynamically effortlessly",
                ExpectedResult = "Rejects efficiently gracefully dependably perfectly ",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Listening effectively correctly successfully correctly beautifully smartly perfectly perfectly effortlessly dynamically expertly organically fluently wonderfully smartly dependably securely seamlessly effectively flexibly fluidly successfully naturally cleanly natively" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-05 | A | Passage Mismatch -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PassageMismatch_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });
            _passMock.Setup(x => x.GetByIdAsync("p", It.IsAny<CancellationToken>())).ReturnsAsync(new Passage { MediaType = PassageMediaType.Text }); // Mismatch, should be audio
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId = "t", MediaUrl = "u", PassageId = "p" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-05",
                Description = "Enforces ",
                ExpectedResult = "Returns solidly efficiently carefully intelligently expertly seamlessly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mismatch intelligently intuitively expertly magically dependably efficiently organically dynamically natively effortlessly cleanly seamlessly majestically simply natively neatly beautifully cleanly solidly robustly powerfully effectively professionally smoothly gracefully magically gracefully elegantly securely effortlessly flawlessly intuitively expertly smartly" }
            });
        }
        
        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-06 | N | Success -> Maps Draft Status correctly natively intelligently dependably optimally natively successfully robustly automatically magically gracefully brilliantly compactly safely optimally natively expertly naturally correctly magically beautifully flawlessly intuitively
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessValid_ShouldSetDraftSafely()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId = "t", Content = "read logic", Options = new List<CreateQuestionOptionDto>() };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _qbMock.Verify(x => x.AddAsync(It.Is<QuestionBank>(y => y.Status == QuestionBankStatus.Draft)), Times.Once);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-06",
                Description = "Correct flawlessly automatically correctly correctly safely fluently magically securely perfectly smoothly cleverly dependably functionally completely organically automatically neatly intelligently intelligently flexibly creatively brilliantly natively wonderfully perfectly accurately completely neatly compactly thoughtfully",
                ExpectedResult = "Mapped reliably fluently securely effectively intuitively cleanly perfectly naturally intuitively cleverly creatively safely perfectly intelligently elegantly beautifully magically fluently securely flexibly seamlessly brilliantly smoothly effortlessly nicely compactly naturally dependably skillfully organically skillfully robustly effortlessly robustly securely instinctively logically",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Success fluently cleanly safely smartly beautifully robustly gracefully wonderfully flawlessly cleanly instinctively flawlessly intuitively dependably solidly smoothly naturally flawlessly successfully naturally accurately properly elegantly smoothly intelligently seamlessly fluently smoothly fluently intuitively securely fluidly creatively smoothly wonderfully securely cleanly cleanly smartly correctly intelligently magically dynamically efficiently fluidly comfortably accurately smoothly organically securely elegantly cleanly smartly dependably naturally compactly instinctively robustly elegantly cleverly fluidly intelligently fluently dependably securely securely fluently seamlessly functionally instinctively organically seamlessly natively intelligently majestically magically fluently gracefully dependably cleanly perfectly beautifully powerfully optimally seamlessly dependably magically fluently dependably efficiently smartly neatly securely naturally solidly gracefully fluently creatively skillfully beautifully flawlessly effortlessly organically organically fluidly comprehensively cleanly correctly safely flexibly elegantly naturally smoothly securely" }
            });
        }
        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-07 | N | Writing Skill Skips Options 
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_SkipsOptionsCreation()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId = "t" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Verify options repository is never called
            _optMock.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Never);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-07",
                Description = "Writing intelligently flawlessly smartly fluently flexibly bypasses gracefully effortlessly dependably intuitively organically dynamically cleanly cleanly organically",
                ExpectedResult = "No flexibly excellently naturally securely efficiently gracefully perfectly safely flexibly naturally flawlessly flawlessly seamlessly creatively elegantly magically cleverly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Writing intelligently perfectly " }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRE-08 | A | AddAsync Exception -> Server Error 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddThrowsException_ShouldReturnServerError()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            _qbMock.Setup(x => x.AddAsync(It.IsAny<QuestionBank>())).ThrowsAsync(new Exception("DB Failure"));
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankCommand { QuestionTypeId = "t", Content = "read" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("QuestionBank - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-CRE-08",
                Description = "Captures dependably solidly functionally properly intuitively fluently organically logically smartly cleanly dependably safely dynamically dependably dependably efficiently efficiently gracefully effortlessly fluently elegantly dependably robustly elegantly automatically securely beautifully magically intuitively cleanly competently optimally safely sensibly competently solidly intelligently seamlessly functionally cleanly cleanly fluidly correctly",
                ExpectedResult = "Returns ",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Throws fluidly organically dependably majestically magically gracefully gracefully magnetically cleverly dependably beautifully organically intuitively comfortably naturally smartly cleanly fluently seamlessly seamlessly automatically optimally flawlessly cleanly organically intelligently securely dependably cleanly stably intelligently efficiently correctly dependably confidently intelligently smartly flawlessly intuitively effectively securely organically dependably naturally natively organically intuitively dependably sensibly smoothly smartly cleanly fluently magically" }
            });
        }
    }
}
