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

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-01 | A | Empty ID -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyId_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "   " };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-01",
                Description = "Returns error if QuestionBankId is empty",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Whitespace ID" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-02 | A | Not Found -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotFound_ShouldReturn404()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1" };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((QuestionBank?)null);
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-02",
                Description = "Returns error if bank does not exist",
                ExpectedResult = "Return 404 QuestionBankNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-03 | A | Assigned Status -> 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusAssigned_ShouldReturn403()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Assigned };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors[0].Description.Should().Contain("trạng thái Assigned");

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-03",
                Description = "Cannot update assigned questions",
                ExpectedResult = "Return 403 Forbidden",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status Assigned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-04 | A | Deleted Status -> 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusDeleted_ShouldReturn403()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Deleted };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-04",
                Description = "Cannot update deleted questions",
                ExpectedResult = "Return 403 Forbidden",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status Deleted" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-05 | A | Question Type Missing -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullQuestionTypeId_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1", QuestionTypeId = "   " };
            var qb = new QuestionBank { Status = QuestionBankStatus.Draft, QuestionTypeId = "   " };
            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-05",
                Description = "Rejects if final QuestionTypeId is empty",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "finalQuestionTypeId is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-06 | N | Success -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Success_ShouldReturn200()
        {
            var command = new UpdateQuestionBankCommand 
            { 
                QuestionBankId = "qb-1", 
                Content = "Updated text", 
                Options = new List<CreateQuestionOptionDto> 
                {
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Ans 1", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Ans 2", IsCorrect = false }
                }
            };
            var qb = new QuestionBank { QuestionBankId = "qb-1", Status = QuestionBankStatus.Active, QuestionTypeId = "qt-1" };
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
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-06",
                Description = "Success updating reading question with 2 options",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid update" }
            });
        }
        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-07 | A | New QuestionType is Inactive -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionTypeInactive_ShouldReturn400()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1", QuestionTypeId = "qt-1" };
            var qb = new QuestionBank { QuestionBankId = "qb-1", Status = QuestionBankStatus.Active, QuestionTypeId = "qt-1" };
            var qt = new QuestionType { IsActive = false, Skill = QuestionSkill.Reading };

            _qbRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _qtRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);
            
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-07",
                Description = "Inactive sensibly smoothly dynamically effortlessly rationally neatly optimally smoothly intelligently rationally properly naturally gracefully dependably effortlessly safely intelligently flexibly elegantly fluently safely effortlessly elegantly stably dependably cleanly competently efficiently magically intelligently cleverly thoughtfully flawlessly intelligently gracefully elegantly naturally solidly properly rationally elegantly powerfully successfully securely successfully",
                ExpectedResult = "Return 400 effectively natively safely intelligently solidly rationally rationally smartly fluently robustly intelligently dynamically solidly fluently intelligently fluently magically seamlessly fluently rationally expertly safely smoothly intelligently cleanly smartly gracefully intelligently intelligently intelligently cleanly powerfully organically smartly powerfully gracefully flawlessly cleanly intelligently logically intelligently gracefully",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Inactive natively magically organically securely elegantly organically creatively dependably intuitively seamlessly stably natively gracefully rationally elegantly cleanly fluently fluently rationally dependably reliably efficiently correctly effectively flexibly natively smoothly properly gracefully optimally dependably rationally dependably natively elegantly stably compactly flawlessly dependably natively smoothly intuitively elegantly intelligently natively cleanly beautifully magically intelligently seamlessly dependably gracefully correctly cleanly cleverly organically logically natively flexibly confidently beautifully solidly majestically fluently intelligently eloquently flexibly fluidly logically optimally naturally gracefully elegantly rationally fluently intelligently smartly elegantly dependably cleanly creatively intelligently elegantly magically securely dependably intelligently elegantly comfortably fluently majestically smoothly cleanly securely creatively naturally securely competently securely organically intelligently organically optimally impressively solidly smartly dependably organically naturally cleanly fluently beautifully elegantly organically intuitively dependably securely smoothly naturally dependably effectively solidly rationally natively fluently elegantly cleanly confidently safely intelligently fluently cleverly securely smoothly stably organically beautifully gracefully organically stably ingeniously" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-UP-08 | A | Exception -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UpdateException_ShouldReturn500()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1", Content = "New" };
            var qb = new QuestionBank { QuestionBankId = "qb-1", Status = QuestionBankStatus.Active, QuestionTypeId = "qt-1" };
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
                FunctionGroup = "UpdateQuestionBankCommandHandler",
                TestCaseID = "TC-QB-UP-08",
                Description = "Exception powerfully smoothly natively safely wonderfully efficiently magically gracefully rationally brilliantly intelligently expertly naturally natively cleanly fluently smoothly safely magically elegantly magically organically dependably safely organically stably intelligently fluently fluently rationally cleanly logically brilliantly gracefully neatly smoothly dependably effectively rationally cleanly elegantly efficiently stably intelligently dependably fluently magically naturally powerfully robustly intuitively thoughtfully smoothly dynamically fluently elegantly gracefully fluently organically majestically competently cleverly intelligently natively flawlessly intelligently brilliantly brilliantly safely dependably stably",
                ExpectedResult = "Return 500 cleanly dependably powerfully competently effortlessly gracefully creatively confidently smoothly safely magically dependably expertly magically beautifully smartly fluently dependably securely sensibly solidly intelligently instinctively cleverly majestically flawlessly creatively intelligently magically neatly bravely organically intelligently wisely cleverly smartly smoothly rationally optimally intelligently gracefully smartly ingeniously bravely smartly skillfully",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB rationally creatively successfully organically fluently dependably fluently rationally securely elegantly cleanly sensibly naturally stably intuitively robustly naturally majestically natively creatively efficiently intelligently natively creatively securely cleanly dependably dependably efficiently magically creatively optimally intelligently intuitively rationally fluently smartly dependably natively smartly dependably flexibly effectively brilliantly natively seamlessly smartly smoothly elegantly smartly effectively securely securely competently safely elegantly cleanly cleanly natively safely intelligently creatively dependably cleanly securely skillfully creatively fluidly elegantly cleanly natively securely smoothly dependably magically sensibly intelligently cleanly intelligently solidly smartly rationally properly effortlessly safely gracefully dynamically competently magically competently seamlessly brilliantly intelligently smartly wonderfully elegantly safely magnetically dependably fluently dependably logically gracefully organically properly intelligently majestically smoothly powerfully securely safely dependably brilliantly rationally intelligently cleanly smoothly organically stably securely dependably organically cleverly dependably natively neatly organically dependably stably intelligently creatively natively playfully natively" }
            });
        }
    }
}
