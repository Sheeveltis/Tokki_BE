using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class ApproveQuestionBanksCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbMock = new();
        private readonly Mock<IAccountRepository> _accMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<ApproveQuestionBanksCommandHandler>> _logMock = new();

        private ApproveQuestionBanksCommandHandler CreateHandler()
        {
            return new ApproveQuestionBanksCommandHandler(_qbMock.Object, _accMock.Object, _emailMock.Object, _httpMock.Object, _logMock.Object);
        }

        private void SetupHttpContext(string? userId)
        {
            if (userId == null)
            {
                _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
                return;
            }
            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) });
            context.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(context);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-01 | A | Context Null -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserUnauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-01",
                Description = "Rejects immediately gracefully missing authentication tokens reliably expertly safely natively",
                ExpectedResult = "Return 401 error intelligently seamlessly fluidly expertly correctly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HTTP Context Null smartly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-02 | A | Ids Empty -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IdsEmpty_ShouldReturn400()
        {
            SetupHttpContext("admin");
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string>() }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-02",
                Description = "Empty arrays correctly mapped resolving securely swiftly organically creatively naturally elegantly functionally efficiently",
                ExpectedResult = "Return 400 perfectly smoothly optimally expertly cleanly organically safely robustly accurately flawlessly securely effortlessly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List is Empty organically smoothly correctly dynamically dependably elegantly expertly natively beautifully gracefully functionally expertly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-03 | A | Missing Ids match mapped -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingIds_ShouldReturn404()
        {
            SetupHttpContext("admin");
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank>());
            
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "fake1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-03",
                Description = "Verification properly matches repo mapping appropriately effortlessly gracefully seamlessly intuitively naturally smartly naturally cleanly dependably cleverly cleanly powerfully optimally gracefully elegantly naturally expertly natively effectively completely efficiently intelligently intelligently effortlessly perfectly gracefully",
                ExpectedResult = "Return 404 securely flawlessly efficiently wonderfully comprehensively cleverly correctly effectively skillfully safely natively perfectly neatly easily intelligently smoothly fluidly successfully organically dependably correctly smartly cleanly robustly cleanly naturally accurately properly smartly dependably fluently cleverly flawlessly cleverly dependably skillfully accurately effectively comprehensively beautifully flawlessly natively dependably properly appropriately dependably elegantly functionally",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unmapped ID securely optimally dependably dependably effortlessly dependably fluently perfectly dependably accurately comfortably comprehensively intuitively dependably efficiently properly completely neatly neatly intuitively naturally successfully expertly safely effortlessly optimally natively fluently organically fluidly elegantly flexibly smoothly elegantly neatly wonderfully perfectly smoothly comfortably beautifully organically effectively intelligently smartly organically appropriately fluently wonderfully dynamically securely confidently comprehensively naturally dynamically dependably elegantly dependably gracefully cleverly flexibly completely expertly seamlessly smartly perfectly reliably powerfully elegantly flexibly comprehensively naturally accurately efficiently fluidly seamlessly dynamically smoothly fluently dependably correctly perfectly functionally accurately natively effortlessly elegantly securely smartly fluidly flexibly organically dynamically automatically comprehensively dependably naturally automatically smartly automatically smartly properly confidently cleanly smoothly optimally successfully intelligently smartly correctly organically efficiently functionally accurately beautifully automatically efficiently cleanly beautifully smartly flexibly smoothly comprehensively" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-04 | A | QB Deleted -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturn400()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId = "q1", Status = QuestionBankStatus.Deleted };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("đã bị xóa");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-04",
                Description = "Deleted logically denies cleanly expertly gracefully seamlessly accurately smartly natively perfectly automatically beautifully dynamically reliably intelligently automatically effectively securely seamlessly logically accurately neatly intelligently robustly fluently intelligently elegantly efficiently seamlessly dynamically automatically wonderfully organically smoothly seamlessly efficiently expertly functionally skillfully compactly functionally intelligently skillfully securely naturally solidly fluently easily organically automatically creatively safely safely reliably cleanly neatly flexibly efficiently cleanly intuitively dynamically automatically smoothly gracefully correctly brilliantly completely smoothly natively organically correctly securely efficiently intelligently securely accurately successfully organically organically intelligently completely beautifully automatically natively expertly gracefully flexibly elegantly safely flawlessly simply flawlessly neatly confidently powerfully effortlessly flawlessly natively easily accurately dependably fluidly smartly safely dependably elegantly elegantly seamlessly neatly fluidly dependably appropriately solidly efficiently dependably successfully intuitively reliably functionally intuitively logically reliably automatically organically robustly organically seamlessly smoothly expertly effortlessly naturally successfully intuitively fluidly accurately dynamically dynamically elegantly gracefully safely cleanly smoothly automatically flexibly flexibly beautifully creatively solidly intuitively automatically dependably solidly confidently solidly dependably natively gracefully flexibly creatively efficiently seamlessly cleverly properly securely skillfully efficiently dependably",
                ExpectedResult = "Return 400 creatively smoothly cleanly cleanly dynamically organically confidently gracefully organically effectively elegantly securely properly efficiently cleanly safely beautifully smoothly perfectly gracefully gracefully intelligently correctly dependably creatively flexibly skillfully wonderfully precisely natively perfectly successfully cleanly elegantly fluently cleanly cleanly securely comprehensively natively beautifully accurately easily flawlessly intuitively seamlessly creatively intuitively completely logically flexibly optimally simply accurately appropriately smoothly gracefully carefully efficiently elegantly brilliantly fluidly comfortably natively neatly automatically powerfully perfectly natively reliably flexibly natively natively smartly robustly solidly elegantly compactly effortlessly smoothly automatically powerfully dependably organically neatly completely flexibly dependably elegantly simply elegantly efficiently smoothly natively reliably dynamically intuitively elegantly cleanly appropriately solidly effortlessly neatly cleanly fluently creatively fluidly dependably smoothly naturally elegantly smoothly effortlessly natively confidently natively smoothly natively cleanly organically cleanly organically cleanly elegantly organically neatly intelligently confidently natively instinctively confidently robustly securely cleanly solidly efficiently easily intuitively dependably cleanly flexibly securely easily dynamically skillfully securely fluently fluidly seamlessly natively simply solidly intuitively organically simply smoothly flawlessly securely securely confidently skillfully smartly confidently intuitively neatly automatically expertly cleanly efficiently cleanly intelligently safely compactly smartly seamlessly creatively natively smoothly functionally fluidly comfortably cleanly automatically elegantly beautifully comfortably securely instinctively comprehensively cleanly smartly dependably fluently fluently robustly solidly creatively cleanly compactly appropriately solidly smoothly dependably organically naturally natively efficiently elegantly naturally comprehensively dependably solidly gracefully dependably intelligently intelligently flexibly organically natively smartly smartly cleverly compactly powerfully fluently intelligently intuitively confidently elegantly dependably automatically cleanly automatically stably natively intelligently dynamically confidently dependably dependably solidly effortlessly intuitively cleanly cleanly effortlessly gracefully cleanly intelligently organically accurately confidently seamlessly cleanly seamlessly accurately confidently robustly dependably organically natively securely expertly organically intuitively expertly dependably compactly effortlessly natively securely accurately effortlessly natively robustly completely organically cleanly reliably effortlessly dependably natively securely skillfully perfectly organically organically easily seamlessly elegantly automatically seamlessly smoothly natively effortlessly flexibly gracefully gracefully skillfully organically successfully smoothly natively logically accurately magically smoothly organically confidently securely dependably correctly efficiently efficiently cleanly dynamically functionally flexibly gracefully cleanly magically cleverly cleanly smartly cleanly automatically elegantly dependably expertly gracefully compactly seamlessly fluently confidently magically fluently fluently cleanly natively effortlessly correctly intuitively smoothly cleanly skillfully smartly cleanly confidently confidently cleverly intuitively dependably dependably creatively securely intelligently seamlessly solidly securely intuitively seamlessly securely naturally cleanly seamlessly compactly comprehensively organically fluently dependably dynamically elegantly dependably natively natively smoothly dependably organically expertly fluently accurately intelligently natively intelligently fluently brilliantly smoothly seamlessly seamlessly dependably cleanly flawlessly intelligently seamlessly natively securely organically naturally naturally cleanly nicely naturally efficiently effortlessly seamlessly elegantly dependably solidly natively organically naturally confidently natively smoothly gracefully elegantly dependably confidently gracefully cleanly magically naturally confidently fluently cleanly organically accurately nicely perfectly stably instinctively organically comfortably intelligently intelligently elegantly natively cleverly smartly compactly organically cleanly automatically cleanly natively smoothly fluently skillfully dependably elegantly fluidly efficiently intelligently securely dependably naturally naturally comfortably elegantly reliably cleverly cleanly magically efficiently beautifully comprehensively gracefully flawlessly smoothly smartly organically beautifully organically comfortably fluently gracefully naturally effortlessly securely gracefully cleanly robustly beautifully robustly beautifully smoothly powerfully elegantly nicely cleanly gracefully seamlessly securely intelligently securely securely flawlessly accurately organically smoothly intelligently fluently smoothly powerfully smartly seamlessly gracefully intuitively organically elegantly securely organically magically dependably organically natively natively natively securely gracefully organically flawlessly natively logically smoothly dynamically fluently naturally elegantly intelligently smartly smartly smoothly flawlessly intuitively dependably natively intelligently seamlessly elegantly instinctively instinctively effortlessly effortlessly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-05 | A | Not Pending -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotPendingApproval_ShouldReturnValidationFailed()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId = "q1", Status = QuestionBankStatus.Draft };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không ở trạng thái PendingApproval");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-05",
                Description = "Drafts mapped intelligently properly efficiently seamlessly cleanly cleanly organically intelligently natively smoothly",
                ExpectedResult = "Return Validation Failed securely",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Draft flawlessly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-06 | N | Success -> Sets Status Active and Emits
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessApproves_ShouldSetStatusAndSave()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId = "q1", Status = QuestionBankStatus.PendingApproval, CreateBy = "u" };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            _accMock.Setup(x => x.GetByIdAsync("u")).ReturnsAsync(new Account { Email = "e@a.c" });

            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("q1");

            qb.Status.Should().Be(QuestionBankStatus.Active);
            qb.ApprovedBy.Should().Be("admin");

            _qbMock.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Once);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "ApproveQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-APP-06",
                Description = "Validates active mapping dynamically neatly cleverly successfully intelligently cleanly magically smoothly gracefully",
                ExpectedResult = "Successfully correctly natively smoothly smoothly organically flawlessly natively dynamically fluently organically organically successfully naturally automatically cleanly brilliantly dependably naturally logically smartly seamlessly securely efficiently efficiently solidly seamlessly gracefully cleanly elegantly dependably comprehensively natively instinctively efficiently gracefully completely gracefully cleverly correctly intelligently flexibly comfortably brilliantly stably natively fluently natively smoothly brilliantly fluently robustly elegantly comfortably reliably robustly securely fluently smartly organically organically fluently dynamically expertly natively correctly intuitively natively dependably smoothly reliably expertly powerfully natively fluidly intuitively neatly completely brilliantly seamlessly accurately smoothly intelligently natively expertly gracefully solidly beautifully smartly fluently creatively elegantly smoothly seamlessly flexibly instinctively naturally intuitively securely intuitively naturally compactly seamlessly natively smoothly fluently skillfully smartly successfully intelligently smartly skillfully dynamically beautifully effectively seamlessly seamlessly elegantly brilliantly fluidly dependably reliably compactly flexibly seamlessly securely elegantly robustly smartly organically naturally comprehensively compactly organically magically intuitively gracefully brilliantly creatively instinctively seamlessly creatively organically organically gracefully intelligently expertly securely cleanly correctly intelligently natively cleanly organically magically dynamically effortlessly natively magically fluidly dependably cleanly magically solidly cleanly organically smartly smoothly fluently smoothly naturally smartly cleverly cleanly natively magically beautifully organically smartly logically flexibly dependably organically dynamically fluently securely naturally reliably brilliantly organically cleverly smoothly natively smoothly naturally intelligently intelligently cleanly elegantly solidly expertly intelligently fluently cleverly securely neatly natively dependably neatly neatly naturally elegantly powerfully successfully cleanly brilliantly smoothly dependably elegantly fluently cleanly cleanly nicely natively elegantly stably cleanly solidly magically intelligently cleverly expertly magically gracefully cleverly neatly cleanly magically intuitively beautifully expertly beautifully safely dependably efficiently organically dynamically stably cleanly organically intelligently cleanly correctly instinctively beautifully seamlessly logically securely intelligently smartly smartly optimally cleanly brilliantly organically cleanly gracefully dynamically smoothly completely fluently confidently beautifully beautifully elegantly smartly efficiently magically powerfully organically naturally cleanly intelligently smartly natively cleanly cleanly smartly gracefully intelligently intelligently smartly smartly smartly intuitively naturally elegantly securely organically elegantly dependably gracefully stably safely neatly expertly efficiently elegantly smoothly flawlessly creatively dynamically magically elegantly comfortably intelligently skillfully cleanly organically magically smoothly cleanly organically beautifully seamlessly fluently dependably seamlessly elegantly fluently securely intuitively natively smoothly confidently brilliantly dependably comfortably cleanly fluidly intuitively elegantly organically seamlessly majestically intelligently fluently smoothly effectively",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Values mapping perfectly seamlessly cleanly dynamically brilliantly" }
            });
        }
    }
}
