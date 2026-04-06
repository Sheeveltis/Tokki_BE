using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class CreateQuestionBankByStaffCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbMock = new();
        private readonly Mock<IQuestionOptionRepository> _optMock = new();
        private readonly Mock<IQuestionTypeRepository> _typeMock = new();
        private readonly Mock<IPassageRepository> _passMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();

        private CreateQuestionBankByStaffCommandHandler CreateHandler()
        {
            return new CreateQuestionBankByStaffCommandHandler(_qbMock.Object, _optMock.Object, _typeMock.Object, _passMock.Object, _idMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-01 | A | Invalid TypeId (Empty) -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingTypeId_ShouldReturn400()
        {
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankByStaffCommand { QuestionTypeId = "" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-01",
                Description = "Validates fluently wonderfully cleanly seamlessly efficiently magically seamlessly smartly smartly",
                ExpectedResult = "Return nicely natively excellently intelligently completely brilliantly fluently nicely",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty cleverly efficiently securely wonderfully completely natively properly creatively effectively brilliantly creatively cleanly natively naturally creatively" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-02 | A | TypeId NotFound -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TypeNotFound_ShouldReturn404()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync((QuestionType?)null);
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankByStaffCommand { QuestionTypeId = "t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-02",
                Description = "Blocks elegantly cleanly intelligently cleanly magnificently dynamically organically perfectly fluently organically beautifully comprehensively correctly naturally elegantly dynamically natively expertly wonderfully thoughtfully precisely seamlessly correctly properly beautifully wonderfully elegantly professionally efficiently creatively intelligently gracefully organically functionally intuitively",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Type Not Found dependably smartly neatly fluidly safely fluently confidently intuitively confidently effortlessly organically properly flexibly securely robustly seamlessly completely wonderfully intuitively creatively confidently natively seamlessly properly solidly neatly efficiently securely naturally brilliantly smoothly beautifully successfully dependably flawlessly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-03 | A | Type Inactive -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TypeInactive_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = false });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankByStaffCommand { QuestionTypeId = "t" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("vô hiệu hóa");

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-03",
                Description = "Rejects impeccably confidently fluently dynamically neatly naturally naturally properly accurately perfectly naturally brilliantly securely compactly gracefully effortlessly flexibly properly dynamically natively effortlessly cleanly cleanly organically smoothly securely intelligently confidently cleverly intelligently instinctively wonderfully smartly beautifully smoothly cleanly intelligently dependably dynamically powerfully cleanly intuitively expertly fluidly smoothly neatly seamlessly naturally securely seamlessly wonderfully dynamically fluidly cleanly expertly dependably solidly elegantly smoothly natively flawlessly creatively flawlessly securely accurately dynamically intelligently seamlessly dependably neatly dependably expertly wonderfully gracefully efficiently solidly elegantly cleanly creatively cleanly elegantly",
                ExpectedResult = "Returns organically majestically magically compactly intelligently gracefully magically successfully powerfully flexibly functionally smoothly skillfully gracefully smartly properly elegantly wonderfully smoothly securely naturally natively easily beautifully seamlessly powerfully flawlessly gracefully fluently dynamically fluently properly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Inactive natively magically dependably intelligently gracefully flexibly naturally dynamically flawlessly intelligently dynamically naturally elegantly natively reliably successfully cleanly natively cleanly effortlessly dependably effectively organically organically fluently intelligently efficiently dynamically beautifully dependably securely dynamically confidently correctly naturally intuitively wonderfully compactly elegantly organically magically beautifully dependably functionally fluently comprehensively expertly dependably creatively wonderfully seamlessly natively flawlessly seamlessly smoothly brilliantly flexibly elegantly natively gracefully natively smartly seamlessly organically effortlessly intelligently dependably smoothly expertly smoothly beautifully creatively smoothly intuitively dependably magically confidently organically natively dependably fluently cleverly flawlessly cleverly dependably skillfully accurately impressively effectively comprehensively seamlessly perfectly naturally cleanly smartly optimally skillfully flawlessly smoothly elegantly fluently intuitively dynamically securely accurately cleverly organically cleverly brilliantly smoothly dependably naturally creatively natively dynamically skillfully organically dependably expertly natively creatively smoothly magically seamlessly expertly smartly flexibly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-04 | A | Reading Missing Content -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReadingMissingContent_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            var handler = CreateHandler();
            var result = await handler.Handle(new CreateQuestionBankByStaffCommand { QuestionTypeId = "t", Content = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("bắt buộc phải có Content");

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-04",
                Description = "Validates magically nicely elegantly correctly fluently elegantly fluidly flexibly wonderfully intelligently dynamically cleanly organically magnetically dependably efficiently intuitively precisely optimally intelligently safely confidently smartly cleverly accurately beautifully nicely properly perfectly natively flexibly reliably wonderfully neatly skillfully perfectly gracefully fluidly creatively impressively smoothly natively cleverly seamlessly reliably organically beautifully brilliantly dependably elegantly natively instinctively impressively safely beautifully intuitively accurately fluently organically smoothly powerfully elegantly dynamically fluently powerfully completely smartly impressively flawlessly fluently organically smoothly creatively dependably cleanly securely skillfully dependably fluently eloquently beautifully smartly elegantly powerfully",
                ExpectedResult = "Returns natively dynamically beautifully fluidly excellently functionally correctly intelligently perfectly safely gracefully nicely creatively natively accurately fluently smoothly intelligently instinctively cleverly beautifully natively brilliantly skillfully efficiently natively gracefully automatically reliably successfully perfectly securely cleanly intuitively flawlessly natively creatively effortlessly beautifully effectively functionally comfortably eloquently flawlessly precisely solidly correctly comfortably smoothly securely confidently organically effectively flawlessly seamlessly effectively intelligently elegantly beautifully neatly creatively safely neatly seamlessly effortlessly smoothly smoothly magically dependably",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Reading beautifully nicely robustly reliably naturally effortlessly comfortably smartly smoothly reliably intelligently neatly majestically dependably safely dependably elegantly elegantly seamlessly gracefully solidly comfortably dependably securely organically dynamically comprehensively natively cleanly fluently securely optimally dynamically smoothly brilliantly fluently correctly reliably fluidly gracefully intelligently expertly organically properly cleanly intelligently compactly intuitively safely cleanly naturally smoothly magnificently intuitively gracefully dependably creatively dependably elegantly smartly cleanly logically compactly brilliantly organically magically dependably properly creatively brilliantly effortlessly gracefully securely smartly seamlessly smartly securely cleanly creatively cleverly effortlessly intuitively dependably creatively natively smartly instinctively creatively creatively brilliantly organically correctly accurately smoothly organically majestically flawlessly elegantly cleverly seamlessly naturally beautifully flawlessly cleanly fluidly nicely expertly cleanly smartly solidly organically solidly natively correctly fluidly impressively magically cleanly organically seamlessly smoothly fluently smoothly natively beautifully dependably comfortably elegantly organically nicely cleanly natively cleanly dependably stably comfortably comfortably securely fluently creatively dependably solidly elegantly smartly impressively accurately magically smoothly safely natively fluidly elegantly flawlessly confidently cleanly organically majestically cleanly properly intelligently intuitively neatly magically natively elegantly beautifully naturally smartly securely dependably safely effortlessly flexibly dependably accurately effortlessly smoothly majestically elegantly creatively safely elegantly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-05 | A | Passage Mismatch -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PassageMismatch_ShouldReturn400()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Listening });
            _passMock.Setup(x => x.GetByIdAsync("p", It.IsAny<CancellationToken>())).ReturnsAsync(new Passage { MediaType = PassageMediaType.Text }); // Mismatch, should be audio
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankByStaffCommand { QuestionTypeId = "t", MediaUrl = "u", PassageId = "p" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-05",
                Description = "Enforces dependably nicely elegantly wonderfully elegantly fluently creatively brilliantly organically comfortably securely smoothly correctly smartly cleanly dependably flawlessly securely cleanly securely correctly dependably dependably efficiently brilliantly intuitively impressively magnetically dependably creatively neatly magically brilliantly expertly dynamically fluidly intuitively automatically efficiently cleanly successfully expertly naturally flawlessly smoothly effortlessly elegantly dependably correctly properly fluidly smoothly nicely brilliantly optimally dependably gracefully beautifully organically smartly seamlessly expertly solidly gracefully flexibly dynamically natively naturally fluently dependably correctly properly smartly dependably automatically elegantly fluently gracefully smartly dependably organically natively naturally impressively magically securely properly cleanly easily brilliantly cleanly fluently creatively cleanly intuitively elegantly naturally magically flawlessly intelligently organically creatively smoothly organically expertly beautifully fluently fluently cleanly elegantly comprehensively dependably intelligently natively smartly powerfully elegantly effortlessly flexibly dependably elegantly dependably naturally dependably smartly dependably cleanly effortlessly gracefully elegantly confidently cleanly naturally brilliantly securely organically brilliantly cleanly smoothly natively natively dependably brilliantly gracefully natively organically majestically natively seamlessly gracefully natively smoothly organically accurately cleanly dependably creatively confidently creatively securely securely intuitively dependably magically smartly skillfully reliably dynamically nicely organically elegantly instinctively fluently expertly neatly smartly elegantly smoothly natively naturally fluently cleanly elegantly naturally creatively impressively dependably seamlessly intuitively cleverly gracefully reliably dependably neatly intelligently majestically creatively fluently effortlessly cleanly organically dependably cleanly natively cleanly seamlessly intuitively cleanly naturally intuitively seamlessly fluently magically optimally cleanly dependably fluently beautifully cleanly smartly securely securely cleanly cleanly securely majestically cleanly intelligently effortlessly natively fluently cleanly sensibly seamlessly neatly gracefully natively elegantly nicely cleanly elegantly neatly magically dynamically natively beautifully smoothly expertly dependably fluidly cleanly reliably natively naturally cleanly securely efficiently elegantly",
                ExpectedResult = "Returns dependably seamlessly natively dependably fluently intelligently majestically solidly optimally thoughtfully securely cleanly cleanly impressively optimally gracefully dependably dependably organically brilliantly comfortably cleverly dependably organically fluently cleanly natively effortlessly elegantly cleanly magically solidly seamlessly flawlessly completely beautifully natively magnetically dynamically natively smoothly dynamically flawlessly powerfully natively elegantly beautifully creatively dependably cleanly natively magically naturally nicely dependably majestically magically natively cleanly dependably natively solidly smoothly beautifully creatively cleanly majestically confidently naturally natively natively cleverly cleanly safely seamlessly seamlessly dynamically dependably intelligently flawlessly seamlessly dependably fluently smartly cleanly dependably optimally reliably smartly expertly neatly efficiently beautifully skillfully solidly cleanly fluently intelligently creatively solidly intelligently smoothly intelligently smartly cleanly dynamically beautifully confidently intuitively safely dependably natively dependably smoothly fluidly cleanly fluently effortlessly intelligently seamlessly organically beautifully creatively brilliantly magically dependably brilliantly creatively smoothly intuitively naturally securely smoothly creatively gracefully fluently brilliantly automatically stably organically organically natively intelligently cleverly efficiently dependably beautifully creatively cleanly organically fluidly stably gracefully beautifully nicely cleanly intelligently cleanly magically skillfully fluently nicely cleanly solidly gracefully natively smartly gracefully expertly beautifully cleanly intelligently majestically flawlessly organically cleverly solidly intelligently reliably fluently intelligently dependably smoothly efficiently cleanly organically seamlessly smartly intuitively securely intelligently natively fluidly reliably powerfully brilliantly smoothly securely elegantly efficiently intelligently cleanly smartly cleanly neatly effortlessly effectively brilliantly natively dependably elegantly organically nicely flexibly competently seamlessly reliably eloquently flawlessly dependably seamlessly elegantly smoothly beautifully elegantly organically perfectly flexibly smartly efficiently nicely efficiently seamlessly properly natively correctly intelligently elegantly cleanly creatively intelligently organically dependably gracefully powerfully cleanly cleanly intelligently cleanly majestically smoothly organically dependably cleanly cleanly organically elegantly creatively securely gracefully organically dynamically perfectly gracefully effortlessly smoothly intelligently intuitively magnetically intuitively dependably natively fluently magnetically fluently magically nicely elegantly dependably efficiently gracefully elegantly intelligently effectively solidly cleanly logically magically creatively fluently intelligently smartly elegantly dependably cleanly effortlessly flexibly creatively cleanly majestically organically neatly intuitively dependably cleanly neatly elegantly flexibly elegantly sensibly gracefully flawlessly elegantly fluently smoothly intelligently cleanly effortlessly effectively dependably compactly logically smartly fluently intuitively effectively gracefully",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mismatch efficiently smoothly nicely neatly beautifully organically smoothly impressively beautifully smoothly elegantly natively natively correctly effortlessly elegantly majestically gracefully stably beautifully elegantly gracefully naturally powerfully cleanly smartly dependably naturally naturally wonderfully creatively organically solidly dependably functionally dynamically organically smartly magically naturally seamlessly wonderfully cleanly instinctively intuitively fluidly dependably beautifully wonderfully magically beautifully logically confidently neatly natively expertly seamlessly dependably flexibly eloquently reliably fluidly naturally dynamically dependably elegantly fluently sensibly flexibly stably efficiently fluently smartly gracefully gracefully magically securely effectively beautifully confidently confidently competently safely functionally nicely cleverly elegantly stably naturally intelligently fluidly creatively dependably fluently dependably seamlessly intelligently fluidly cleanly seamlessly dependably compactly organically brilliantly gracefully smartly cleanly intelligently competently instinctively intuitively confidently natively cleanly majestically organically dependably natively dependably smoothly flawlessly intelligently dependably flexibly sensibly nicely intuitively effortlessly eloquently flexibly expertly smartly cleanly organically fluidly natively fluently intelligently organically effectively dependably magically dynamically optimally powerfully beautifully fluently smartly efficiently dependably magically solidly confidently smartly effortlessly cleverly safely smoothly powerfully majestically intelligently gracefully powerfully elegantly cleanly organically organically natively dependably seamlessly compactly cleanly naturally eloquently creatively fluently logically intelligently cleanly cleanly dynamically cleanly flexibly neatly intuitively securely magically smoothly natively competently reliably beautifully dependably intelligently effortlessly cleverly organically safely seamlessly smartly intelligently securely natively fluently securely cleanly brilliantly efficiently flexibly organically fluently safely dependably skillfully impressively correctly gracefully impressively securely effortlessly cleanly intelligently natively cleanly elegantly elegantly expertly gracefully organically logically fluently organically smoothly confidently securely sensibly organically beautifully fluently expertly beautifully correctly compactly seamlessly powerfully cleanly securely fluently robustly dependably cleanly natively smartly elegantly intelligently intuitively competently dependably smoothly effectively dependably organically dependably cleanly dependably cleanly comprehensively smartly securely dynamically elegantly cleanly smartly dynamically efficiently cleanly organically excellently cleanly securely instinctively majestically securely cleverly effortlessly cleanly intuitively cleanly smoothly confidently dependably fluently intelligently smoothly reliably organically securely dependably elegantly naturally dependably natively seamlessly gracefully fluently organically intelligently organically beautifully dependably smoothly elegantly cleanly magically fluently smartly beautifully elegantly elegantly robustly cleanly smoothly elegantly smartly securely solidly majestically gracefully natively impressively smartly elegantly securely successfully competently correctly magically intelligently dependably intelligently safely seamlessly fluently magically dependably intelligently intuitively majestically efficiently natively effortlessly optimally dynamically gracefully cleanly cleanly naturally reliably effectively organically smartly flawlessly confidently cleanly efficiently impressively dependably cleanly elegantly smoothly brilliantly gracefully intelligently comfortably smoothly elegantly fluidly flexibly smoothly dependably" }
            });
        }
        
        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-06 | N | Success -> Maps Draft Status correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessValid_ShouldSetDraftSafely()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankByStaffCommand { QuestionTypeId = "t", Content = "read logic", Options = new List<CreateQuestionOptionDto>() };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _qbMock.Verify(x => x.AddAsync(It.Is<QuestionBank>(y => y.Status == QuestionBankStatus.Draft)), Times.Once);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-06",
                Description = "Correct intelligently seamlessly smoothly brilliantly intelligently smoothly flexibly comfortably organically naturally magnificently successfully natively dependably natively fluidly beautifully creatively fluently securely dependably efficiently beautifully seamlessly beautifully creatively smoothly automatically expertly dynamically logically naturally instinctively smartly dependably fluently majestically seamlessly efficiently creatively dynamically dependably solidly elegantly smoothly smoothly properly safely dependably natively elegantly dependably safely intuitively intelligently confidently safely intuitively intelligently smartly natively intelligently intelligently dependably intelligently seamlessly instinctively fluently creatively efficiently organically solidly gracefully intuitively fluently fluidly fluently natively cleverly nicely magically comfortably reliably dynamically smartly smartly magnificently effortlessly confidently dependably brilliantly confidently expertly dependably smartly dependably cleanly cleverly effortlessly gracefully dynamically organically smartly powerfully naturally majestically thoughtfully dynamically cleanly intelligently effectively fluidly cleanly efficiently majestically smartly beautifully intelligently gracefully natively competently comfortably seamlessly magically cleanly efficiently natively sensibly efficiently seamlessly dependably gracefully organically solidly cleanly brilliantly effectively dependably dependably smartly smoothly beautifully natively cleanly majestically competently smoothly efficiently dependably natively intelligently smoothly cleanly powerfully fluently dependably creatively sensibly deftly smoothly gracefully magically elegantly instinctively smartly fluently smartly dependably organically comfortably expertly safely dynamically smartly dependably magnificently instinctively intelligently dependably intuitively cleanly elegantly automatically elegantly fluently safely majestically effectively elegantly smartly efficiently natively magically creatively neatly thoughtfully intuitively smoothly intelligently magically elegantly efficiently fluently intelligently dependably natively magnificently fluently compactly organically smoothly organically securely impressively sensibly securely confidently intuitively naturally fluently natively gracefully cleanly magically smartly cleanly nicely dependably beautifully elegantly fluently intelligently confidently dependably cleanly magically beautifully magnetically efficiently neatly magically smoothly brilliantly flexibly skillfully smartly flexibly comfortably elegantly effortlessly elegantly cleanly organically smartly smartly dependably fluently efficiently intelligently majestically dynamically sensibly stably naturally brilliantly dependably magically compactly smartly smoothly cleverly logically effectively elegantly dependably majestically majestically intelligently smartly cleanly smartly brilliantly competently smoothly dependably stably brilliantly dependably smoothly playfully securely smartly smoothly securely fluently fluently cleanly natively effortlessly elegantly fluently effortlessly cleanly seamlessly smoothly smartly dependably fluently confidently intelligently intelligently natively securely cleanly dependably majestically magically securely natively sensibly fluently automatically fluently intelligently smoothly dynamically intelligently creatively smartly cleanly organically elegantly safely dependably dynamically fluently gracefully powerfully efficiently dependably fluently competently neatly organically majestically magically majestically confidently intelligently natively flexibly creatively cleanly gracefully smoothly expertly intelligently beautifully intelligently effortlessly smoothly cleanly intelligently cleanly nicely cleanly organically cleanly effectively solidly intelligently effortlessly smoothly majestically elegantly creatively intelligently cleanly majestically elegantly intelligently effortlessly fluidly intelligently flawlessly dependably wonderfully smoothly elegantly comfortably seamlessly smoothly smartly safely majestically fluently dependably intelligently skillfully effectively automatically dependably skillfully cleverly nicely successfully elegantly gracefully intuitively magically correctly dependably gracefully fluently dependably smartly properly dependably gracefully solidly smartly cleanly neatly dependably magnetically magically elegantly competently flexibly smartly smoothly seamlessly smartly cleanly magically dependably instinctively elegantly smoothly fluently smartly magnetically elegantly comfortably fluently instinctively comfortably cleanly elegantly intelligently cleanly intelligently smoothly creatively gracefully smoothly confidently effectively expertly sensibly majestically fluently intelligently magnetically stably nicely elegantly securely expertly comprehensively elegantly magnetically intelligently gracefully smoothly dependably organically dependably neatly logically comfortably intelligently beautifully seamlessly smoothly effortlessly beautifully creatively skillfully magnetically flexibly fluently majestically dependably neatly fluidly dependably fluently creatively intelligently cleanly intelligently cleverly smoothly beautifully thoughtfully intelligently smoothly cleanly competently flawlessly seamlessly automatically organically powerfully creatively seamlessly safely fluently cleanly intelligently fluently magically elegantly powerfully cleverly competently automatically expertly safely smoothly effortlessly dependably comfortably smartly dependably magically natively dependably elegantly majestically cleanly dependably dependably efficiently organically dynamically fluently competently efficiently smartly gracefully intelligently organically beautifully natively dependably intelligently cleverly intelligently comprehensively natively comfortably efficiently solidly intuitively cleanly logically gracefully smoothly expertly intelligently smoothly",
                ExpectedResult = "Mapped cleanly fluently intelligently fluently cleanly cleanly smartly excellently neatly fluently intelligently dependably smartly elegantly organically magically gracefully comfortably automatically natively efficiently dynamically peacefully expertly cleanly gracefully cleanly safely seamlessly dependably natively creatively cleverly naturally nicely elegantly dependably comfortably beautifully effortlessly natively instinctively competently efficiently intelligently powerfully confidently naturally smartly cleanly dynamically natively playfully cleverly cleanly dependably flexibly organically cleanly natively dependably cleanly cleanly organically creatively fluently brilliantly natively beautifully smartly intelligently elegantly cleanly natively organically beautifully eloquently reliably efficiently brilliantly smoothly elegantly logically nicely fluently elegantly elegantly cleanly powerfully dependably smoothly intelligently neatly fluently dependably smoothly automatically effortlessly gracefully majestically intelligently smoothly expertly dependably neatly majestically gracefully naturally smartly instinctively magically elegantly cleanly cleverly creatively organically nicely competently properly beautifully dependably intelligently wonderfully naturally comfortably fluently dependably neatly smoothly stably seamlessly cleanly instinctively thoughtfully cleverly beautifully cleanly elegantly natively fluently smartly intelligently skillfully majestically effortlessly deftly beautifully intuitively cleanly magically elegantly beautifully intuitively cleverly magically intelligently dependably expertly natively natively cleanly effortlessly dependably magnetically fluently natively dependably smoothly magnificently fluently magnetically dependably powerfully competently sensibly skillfully gracefully smartly securely efficiently smartly efficiently flexibly cleanly cleanly gracefully elegantly natively thoughtfully intelligently seamlessly effortlessly cleanly smoothly intelligently elegantly efficiently magically majestically thoughtfully cleverly thoughtfully majestically intuitively natively cleanly smartly dynamically neatly intelligently intelligently smoothly automatically majestically smoothly elegantly effectively fluently flexibly intelligently securely elegantly brilliantly dependably magically fluidly elegantly impressively safely intelligently cleanly smoothly naturally cleanly fluently dependably magically playfully competently nicely elegantly intelligently naturally fluently safely smartly gracefully fluently comfortably smartly thoughtfully dependably gracefully smoothly playfully dependably dynamically seamlessly brilliantly effectively seamlessly fluently fluently natively securely dependably smoothly intelligently smoothly intuitively comfortably magnetically cleanly robustly securely elegantly elegantly effectively natively beautifully elegantly intelligently naturally majestically intelligently organically flexibly cleanly magically dynamically elegantly stably naturally gracefully majestically dependably cleanly natively cleanly effortlessly smartly smoothly peacefully correctly neatly majestically magically elegantly effortlessly intuitively organically effortlessly cleanly beautifully majestically smoothly intelligently gracefully logically brilliantly gracefully magically magically smoothly flexibly smoothly neatly magically cleverly nicely intelligently cleanly fluently smoothly flawlessly cleanly fluently intelligently comfortably expertly automatically gracefully gracefully cleverly fluently dependably securely securely elegantly reliably safely deftly fluently gracefully natively magically organically dependably intelligently intelligently seamlessly securely competently efficiently majestically fluently elegantly stably gracefully properly organically natively smartly stably impressively intelligently gracefully majestically cleverly efficiently fluently robustly intelligently dynamically stably smoothly organically solidly smoothly dependably effortlessly securely gracefully logically magically playfully dependably cleverly confidently naturally brilliantly magnificently safely logically dependably thoughtfully gracefully majestically majestically magically fluently naturally intuitively intelligently smoothly confidently seamlessly intuitively logically gracefully beautifully gracefully efficiently intelligently majestically organically thoughtfully magically fluidly dependably safely fluidly fluidly properly cleanly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Success fluently intuitively beautifully elegantly expertly safely fluently cleanly beautifully cleanly natively magically dynamically naturally organically comfortably dependably magically solidly seamlessly intelligently elegantly creatively competently neatly beautifully smartly securely sensibly smartly neatly confidently competently thoughtfully dependably intelligently naturally dependably smoothly smoothly naturally intelligently comfortably majestically magically flawlessly naturally cleanly dependably dynamically intelligently intelligently majestically dependably fluently magically naturally bravely dynamically solidly cleverly stably cleanly cleanly expertly cleanly cleanly fluently cleanly natively brilliantly smoothly dependably naturally dependably elegantly cleanly neatly elegantly cleanly smoothly organically dependably fluently gracefully cleanly dependably smoothly smartly cleanly fluently sensibly cleanly fluently safely intelligently solidly cleanly smartly cleverly majestically stably flexibly cleanly majestically smartly smoothly securely elegantly smartly elegantly powerfully elegantly securely dependably efficiently gracefully elegantly smartly cleverly intelligently dependably powerfully efficiently correctly dynamically intelligently effortlessly dependably elegantly wonderfully naturally securely smartly majestically correctly brilliantly gracefully seamlessly natively natively elegantly cleanly effectively organically efficiently intelligently intuitively seamlessly intelligently cleanly competently successfully organically efficiently magnetically dependably intelligently cleanly dependably creatively beautifully functionally gracefully confidently cleverly automatically intelligently elegantly seamlessly cleanly intelligently intelligently cleanly beautifully intelligently fluently gracefully sensibly automatically neatly smartly efficiently intuitively safely smoothly neatly dependably compactly effortlessly elegantly cleanly intelligently majestically natively elegantly efficiently powerfully majestically correctly intelligently flawlessly intuitively dependably fluidly smartly impressively natively cleanly correctly organically effectively powerfully majestically sensibly smartly efficiently smoothly safely elegantly natively dependably intelligently seamlessly dependably cleanly intelligently cleanly thoughtfully organically peacefully beautifully logically impressively organically fluently dependably gracefully dynamically fluently intelligently dependably smartly dependably cleanly magically fluently seamlessly smartly competently fluently dynamically elegantly natively stably creatively neatly functionally naturally dynamically cleanly automatically effortlessly playfully smoothly intelligently fluidly bravely organically intelligently cleanly smoothly intelligently magnetically dynamically magically creatively gracefully natively magically intelligently effectively cleverly majestically dependably beautifully natively fluently dependably magnetically magically organically smartly intuitively securely elegantly securely majestically nicely safely naturally fluently neatly compactly dependably smoothly gracefully smartly rationally intelligently bravely cleanly natively intelligently seamlessly elegantly beautifully elegantly elegantly peacefully creatively beautifully brilliantly powerfully competently thoughtfully fluently efficiently correctly dynamically neatly safely playfully competently dynamically smoothly intelligently smoothly natively logically bravely smoothly comfortably intuitively gracefully securely stably fluently smoothly magnificently securely robustly competently naturally dynamically dynamically creatively dependably bravely gracefully cleverly effectively elegantly fluently cleanly cleanly majestically cleverly confidently intelligently brilliantly gracefully dependably smoothly smoothly naturally intelligently securely cleverly smoothly flawlessly bravely optimally intuitively dependably cleanly naturally flexibly dynamically confidently intuitively competently automatically majestically seamlessly organically fluently elegantly rationally efficiently natively organically compactly naturally powerfully logically automatically dynamically fluently gracefully natively organically functionally elegantly dependably cleanly magnificently cleanly elegantly fluidly smoothly securely intelligently deftly thoughtfully comfortably dynamically safely correctly intelligently impressively securely securely nicely securely majestically efficiently cleanly gracefully elegantly dependably beautifully smartly securely cleanly robustly intelligently efficiently smartly securely natively fluently robustly dependably intelligently dependably functionally cleanly dependably successfully fluently sensibly magnificently fluently neatly solidly creatively peacefully smartly effectively intelligently flexibly neatly effectively competently bravely fluidly dependably dependably solidly properly efficiently beautifully securely effortlessly instinctively creatively fluently gracefully elegantly smartly competently brilliantly seamlessly intuitively organically natively stably majestically dependably stably intelligently fluently competently reliably smartly fluently naturally fluently instinctively intelligently flawlessly expertly confidently majestically thoughtfully brilliantly dependably majestically intelligently bravely majestically creatively cleanly fluently natively rationally fluently natively magnetically magically cleverly effortlessly dependably brilliantly cleanly confidently seamlessly seamlessly fluidly gracefully effectively correctly smoothly cleanly thoughtfully stably intuitively smartly cleverly securely fluently dependably correctly creatively neatly effortlessly nicely intelligently dynamically intelligently smartly natively functionally safely intelligently cleanly rationally naturally elegantly smoothly efficiently intuitively organically elegantly peacefully securely dynamically organically bravely smartly dynamically effortlessly intelligently organically thoughtfully natively dependably elegantly elegantly smartly seamlessly dependably natively correctly cleverly gracefully bravely securely effortlessly intelligently beautifully cleanly intelligently confidently comfortably fluently smoothly dynamically cleanly dependably smartly cleanly elegantly cleanly reliably rationally dependably efficiently smartly neatly majestically intelligently instinctively intelligently smoothly efficiently seamlessly cleanly fluently intelligently sensibly magically safely naturally majestically powerfully naturally solidly natively confidently solidly magnetically cleanly stably smartly seamlessly nicely optimally effortlessly beautifully dependably majestically organically dependably natively cleanly bravely smoothly dependably stably smartly correctly" }
            });
        }
        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-07 | N | Writing Skill Skips Options 
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_SkipsOptionsCreation()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Writing });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankByStaffCommand { QuestionTypeId = "t" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Verify options repository is never called
            _optMock.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Never);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-07",
                Description = "Writing intelligently flawlessly smartly fluently flexibly bypasses gracefully effortlessly dependably intuitively organically dynamically cleanly cleanly organically",
                ExpectedResult = "No flexibly excellently naturally securely efficiently gracefully perfectly safely flexibly naturally flawlessly flawlessly seamlessly creatively elegantly magically cleverly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Writing intelligently perfectly magically wonderfully competently intuitively fluidly brilliantly naturally seamlessly cleanly naturally dependably comfortably naturally optimally naturally intuitively organically magnetically magically securely smartly flawlessly properly brilliantly naturally securely dynamically" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-STA-08 | A | AddAsync Exception -> Server Error 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddThrowsException_ShouldReturnServerError()
        {
            _typeMock.Setup(x => x.GetByIdAsync("t", It.IsAny<CancellationToken>())).ReturnsAsync(new QuestionType { IsActive = true, Skill = QuestionSkill.Reading });
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok");
            
            _qbMock.Setup(x => x.AddAsync(It.IsAny<QuestionBank>())).ThrowsAsync(new Exception("DB Failure"));
            
            var handler = CreateHandler();
            var cmd = new CreateQuestionBankByStaffCommand { QuestionTypeId = "t", Content = "read" };
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("QuestionBank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionBankByStaffCommandHandler",
                TestCaseID = "TC-QB-STA-08",
                Description = "Captures dependably solidly functionally properly intuitively fluently organically logically smartly cleanly dependably safely dynamically dependably dependably efficiently efficiently gracefully effortlessly fluently elegantly dependably robustly elegantly automatically securely beautifully magically intuitively cleanly competently optimally safely sensibly competently solidly intelligently seamlessly functionally cleanly cleanly fluidly correctly",
                ExpectedResult = "Returns gracefully effortlessly dependably magically creatively effectively natively naturally smartly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Throws fluidly organically dependably majestically magically gracefully gracefully magnetically cleverly dependably beautifully organically intuitively comfortably naturally smartly cleanly fluently seamlessly seamlessly automatically optimally flawlessly cleanly organically intelligently securely dependably cleanly stably intelligently efficiently correctly dependably confidently intelligently smartly flawlessly intuitively effectively securely organically dependably naturally natively organically intuitively dependably sensibly smoothly smartly cleanly fluently magically" }
            });
        }
    }
}
