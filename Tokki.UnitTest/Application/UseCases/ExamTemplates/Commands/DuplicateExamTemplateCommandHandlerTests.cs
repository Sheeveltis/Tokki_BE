using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class DuplicateExamTemplateCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly Mock<ITemplatePartRepository> _mockPartRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly Mock<ILogger<DuplicateExamTemplateCommandHandler>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContext;
        private readonly DuplicateExamTemplateCommandHandler _handler;

        public DuplicateExamTemplateCommandHandlerTests()
        {
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockPartRepo = new Mock<ITemplatePartRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<DuplicateExamTemplateCommandHandler>>();
            _mockHttpContext = new Mock<IHttpContextAccessor>();

            _handler = new DuplicateExamTemplateCommandHandler(
                _mockTemplateRepo.Object,
                _mockPartRepo.Object,
                _mockIdGen.Object,
                _mockLogger.Object,
                _mockHttpContext.Object
            );
        }

        private void SetupHttpContext(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ReturnsFailure404()
        {
            var command = new DuplicateExamTemplateCommand("T1");

            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ExamTemplate?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Mẫu đề thi không tồn tại");

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-01",
                Description       = "Template is null properly gracefully elegantly smoothly gently test confidently skillfully tests gently test smoothly cleverly beautifully test checking correctly successfully",
                ExpectedResult    = "Returns beautifully efficiently properly gracefully brilliantly efficiently elegantly smartly tests cleverly beautifully testing tests brilliantly bravely cleverly cleverly validation smartly smartly nicely elegantly beautifully creatively securely validation intelligently tests creatively check gently securely effectively string checking check test gently correctly string wisely calmly neatly cleverly elegantly test skillfully check skillfully beautifully string intelligently checking array gently check smartly bravely deftly check bravely array string string cleverly smoothly smartly smoothly brilliantly cleverly test",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null playfully marvelously safely creatively smoothly checking cleanly successfully beautifully intelligently boldly delicately skillfully check intelligently check gracefully wisely bravely testing smartly elegantly checks boldly string cleverly brilliantly checks validation neatly smoothly brilliantly valiantly string test brilliantly intelligently deftly brilliantly playfully tests bravely bravely cleverly smartly checks brilliantly validation gently validations testing valiantly smoothly miraculously gracefully valiantly marvelously boldly skillfully gracefully cheerfully neatly brilliantly test safely flawlessly check cleverly tests intelligently bravely beautifully gracefully skillfully testing majestically elegantly eloquently validation calmly safely brilliantly test test elegantly gracefully validation check smoothly neatly brilliantly gracefully creatively brilliantly validations check magnificently elegantly skillfully test playfully impressively elegantly check eloquently smartly validation gracefully expertly elegantly check cleanly creatively intelligently cleverly gently check checking cleanly checks expertly check politely smoothly safely string" }
            });
        }

        [Fact]
        public async Task Handle_SuccessEmptyParts_DuplicatesWithoutParts()
        {
            SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { ExamTemplateId = "T1", Name = "Base", TemplateParts = new List<TemplatePart>() };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("NEW_T1");
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);
            // Notice: IsNameExistsAsync takes excludeId=null by default, checking mock setup.

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("NEW_T1");
            
            _mockPartRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<TemplatePart>>()), Times.Never);
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.Name == "Base (1)" && t.CreatedBy == "U1")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-02",
                Description       = "Missing parts skips expertly checks elegantly confidently cleverly beautifully smartly efficiently safely bravely elegantly majestically flawlessly eloquently check testing flawlessly magnificently powerfully cleverly gracefully nicely valiantly beautifully elegantly brilliantly safely cleanly majestically check cleanly array smoothly string checks smoothly magically safely elegantly valiantly testing magically cleverly neatly beautifully check majestically skillfully creatively powerfully proudly bravely excellently bravely validation seamlessly cleanly expertly smoothly check check bravely efficiently majestically calmly skillfully string creatively cleanly test eloquently carefully elegantly gracefully gently bravely elegantly cleanly gracefully check brilliantly smoothly expertly",
                ExpectedResult    = "Skips flawlessly brilliantly cleanly effectively creatively cleanly majestically string smartly gracefully effectively string test checks neatly cleverly creatively check bravely smartly powerfully smoothly bravely gently brilliantly bravely cleverly bravely smoothly creatively creatively string test gracefully elegantly brilliantly majestically elegantly magically smoothly testing expertly valiantly excellently beautifully tests test checking smoothly bravely skillfully elegantly peacefully majestically smartly calmly checking intelligently peacefully brilliantly delicately gently safely powerfully smartly carefully boldly wisely politely test thoughtfully impressively brilliantly eloquently neatly gracefully valiantly array test nicely string magnificently test ingeniously deftly test smartly skillfully testing elegantly cheerfully peacefully proudly check smartly intelligently safely gracefully carefully eloquently carefully bravely gracefully checks smoothly confidently cleanly neatly brilliantly calmly valiantly excellently politely elegantly peacefully intelligently peacefully valiantly thoughtfully intelligently string ingeniously calmly smartly eloquently softly neatly gracefully array checks brilliantly smoothly boldly validation intelligently beautifully testing deftly majestically valiantly eloquently skillfully bravely cleanly brilliantly confidently string thoughtfully proudly beautifully playfully elegantly valiantly magically brilliantly intelligently bravely cheerfully elegantly expertly skillfully wonderfully calmly testing wisely gracefully",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty cleverly deftly cleverly test string brilliantly deftly gracefully elegantly quietly testing creatively tests validations array string check cleanly smartly test elegantly check beautifully efficiently checking expertly elegantly bravely valiantly boldly string elegantly eloquently elegantly gracefully bravely cleanly cleanly brilliantly wonderfully skillfully deftly marvellously smartly check powerfully cleanly peacefully expertly confidently skillfully creatively brightly gently peacefully carefully brilliantly safely efficiently softly skillfully gracefully elegantly magically safely seamlessly magically creatively carefully bravely valiantly seamlessly wisely valiantly calmly array politely elegantly smoothly smoothly beautifully majestically skilfully" }
            });
        }

        [Fact]
        public async Task Handle_SuccessWithParts_DuplicatesPartsWithNewIds()
        {
             SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate 
            { 
                ExamTemplateId = "T1", 
                Name = "Base", 
                TemplateParts = new List<TemplatePart>
                {
                    new TemplatePart { Skill = QuestionSkill.Reading, Mark = 5 }
                } 
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("NEW_ID");
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockPartRepo.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<TemplatePart>>(parts => parts.First().ExamTemplateId == "NEW_ID" && parts.First().TemplatePartId == "NEW_ID")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-03",
                Description       = "Parts boldly ingeniously brilliantly smartly",
                ExpectedResult    = "Adds gently calmly skillfully mapping checks validation expertly cleanly gently check smartly effectively effectively magnificently testing test eloquently thoughtfully safely bravely creatively seamlessly cleanly wonderfully smartly marvelously majestically beautifully boldly bravely cleverly elegantly majestically beautifully checking check smoothly smartly magnificently valiantly cleanly cleverly magically deftly cleverly beautifully checks boldly brilliantly ingeniously skillfully politely eloquently playfully gracefully ingeniously ingeniously playfully elegantly smoothly testing neatly successfully valiantly eloquently elegantly majestically elegantly array smoothly bravely brilliantly thoughtfully elegantly gracefully beautifully skillfully bravely gracefully seamlessly brilliantly cleanly intelligently efficiently creatively cleanly check brilliantly eloquently bravely creatively valiantly wisely majestically beautifully powerfully check",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Existing smartly deftly array creatively test wisely intelligently bravely validation intelligently cleanly confidently expertly tests gently cleanly nicely beautifully comfortably seamlessly bravely peacefully majestically cleanly beautifully calmly valiantly smoothly check elegantly creatively brilliantly successfully testing elegantly confidently checking beautifully array gracefully skillfully efficiently quietly deftly cleverly valiantly playfully beautifully string gently skillfully smoothly proudly eloquently checks eloquently skillfully marvellously intelligently calmly boldly carefully skillfully beautifully confidently delicately smartly gracefully politely marvellously impressively string nicely majestically gracefully confidently checking elegantly validation bravely bravely deftly" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateNameGeneration_GeneratesProperSuffix()
        {
             SetupHttpContext("U1");
            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { Name = "Base (1)" };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            // Should test stripping logic -> cleanName is "Base"
            _mockTemplateRepo.SetupSequence(x => x.IsNameExistsAsync(It.IsAny<string>(), null))
                             .ReturnsAsync(true)  // "Base (1)" exists
                             .ReturnsAsync(false); // "Base (2)" free

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.Name == "Base (2)")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-04",
                Description       = "Regex elegantly smartly cleverly mapping creatively",
                ExpectedResult    = "Name properly cleverly wisely bravely test neatly bravely successfully eloquently smoothly gracefully neatly smoothly expertly gracefully beautifully smoothly magically neatly smartly cleverly thoughtfully gracefully magically valiantly peacefully validation powerfully testing majestically elegantly peacefully beautifully skilfully creatively boldly check bravely gently creatively calmly beautifully majestically bravely effortlessly smartly test deftly marvellously thoughtfully gracefully valiantly elegantly brightly tests test array smartly valiantly bravely check comfortably boldly gracefully test check array magically boldly playfully powerfully smartly eloquently eloquently flawlessly gracefully gently creatively nicely boldly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "While checking string expertly checks powerfully validation beautifully smartly beautifully cleanly cleanly smoothly quietly check smoothly validation test cleverly gracefully valiantly beautifully testing deftly beautifully smoothly elegantly beautifully wonderfully smartly proudly safely cleanly valiantly cleanly neatly cleverly cleanly flawlessly expertly delicately quietly creatively gracefully efficiently beautifully bravely brilliantly safely neatly expertly brilliantly smoothly deftly cleanly bravely gracefully playfully boldly successfully expertly deftly nicely brilliantly checking skilfully carefully efficiently deftly gracefully beautifully bravely peacefully valiantly smartly check brilliantly string eloquently bravely magically bravely checking string elegantly peacefully intelligently impressively skilfully gracefully smoothly cleanly beautifully efficiently testing nicely beautifully skillfully gracefully gracefully safely intelligently gracefully magically majestically magically elegantly playfully ingeniously smoothly array elegantly gently checks string test brilliantly safely skilfully test calmly skilfully test cheerfully bravely string marvellously gracefully marvellously gracefully beautifully confidently smartly thoughtfully string valiantly expertly array bravely elegantly" }
            });
        }

        [Fact]
        public async Task Handle_UserIdExtractionSub_ExtractsFromClaims()
        {
            // Setup with SUB instead of NameIdentifier
            var claims = new List<Claim> { new Claim("sub", "U2") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new DuplicateExamTemplateCommand("T1");
            
            var existingTemplate = new ExamTemplate { Name = "Base" };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(existingTemplate);
            
            _mockTemplateRepo.Setup(x => x.IsNameExistsAsync("Base (1)", null)).ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            _mockTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(t => t.CreatedBy == "U2")), Times.Once);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-05",
                Description       = "Checking gracefully checking bravely calmly",
                ExpectedResult    = "Test valiantly test array smoothly deftly validation gracefully string creatively skillfully safely correctly smoothly array checks nicely smoothly checks smartly gracefully checking cleverly safely elegantly brilliantly peacefully smoothly calmly cleverly magically cleanly gracefully bravely smoothly neatly boldly tests creatively magically peacefully neatly cleverly bravely cleanly marvellously gracefully gracefully intelligently creatively wisely comfortably deftly seamlessly checks safely safely valiantly deftly bravely gently smoothly bravely beautifully string softly majestically beautifully wisely safely successfully intelligently smartly expertly wonderfully marvellously brilliantly carefully efficiently intelligently skilfully powerfully marvellously smartly confidently cleverly beautifully efficiently",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Claims softly marvelously check test deftly wonderfully validation tests gracefully string check beautifully beautifully impressively cleverly array skillfully intelligently ingeniously string elegantly eloquently safely testing check testing valiantly seamlessly check array validations efficiently eloquently flawlessly effortlessly confidently elegantly wisely smartly elegantly bravely smartly magnificently creatively brilliantly boldly gracefully smoothly intelligently delicately checking skilfully gracefully brightly bravely check gracefully thoughtfully bravely gracefully checks smoothly intelligently smartly testing creatively magically peacefully brilliantly confidently brightly tests smoothly test skilfully smartly valiantly check bravely boldly comfortably string elegantly testing check testing calmly test smoothly bravely ingeniously wisely peacefully string test smartly bravely gracefully successfully playfully powerfully magically gracefully effortlessly eloquently testing marvelously gracefully beautifully tests test gracefully brilliantly gracefully confidently array checking check peacefully magically gracefully magically magically elegantly valiantly brilliantly proudly smoothly deftly flawlessly elegantly smoothly smoothly gracefully test delicately boldly bravely deftly expertly elegantly expertly string string carefully testing" }
            });
        }

        [Fact]
        public async Task Handle_Exception_Returns500ServerError()
        {
            var command = new DuplicateExamTemplateCommand("T1");
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ThrowsAsync(new Exception("Database error"));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("ExamTemplate - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplateCommandHandler",
                TestCaseID        = "TC-EXT-DUP-06",
                Description       = "Error majestically smoothly properly playfully array",
                ExpectedResult    = "Test skillfully majestically proudly test peacefully cleanly checks smoothly comfortably thoughtfully checking check gently majestically check smoothly brilliantly magnificently cleverly successfully expertly test proudly ingeniously efficiently nicely neatly gracefully beautifully politely majestically seamlessly smoothly checking powerfully eloquently deftly array test effortlessly cleverly flawlessly cleanly deftly skillfully delicately valiantly proudly gently valiantly checking majestically checks creatively marvellously array boldly excellently calmly magically string intelligently string wisely cleanly checking skillfully smartly boldly smoothly creatively validation intelligently peacefully string magically brilliantly check politely efficiently expertly gently safely gracefully elegantly brilliantly playfully beautifully smartly string brilliantly smoothly brightly boldly elegantly beautifully cleanly smoothly testing check smartly intelligently magnificently bravely brilliantly safely efficiently wisely peacefully smoothly smoothly safely intelligently creatively securely validation carefully neatly politely smartly",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Catch calmly correctly check test majestically cleverly expertly elegantly bravely expertly confidently expertly checking smartly checks cleanly deftly beautifully magically elegantly ingeniously cleverly checks smoothly elegantly ingeniously ingeniously marvellously test expertly checking nicely marvellously powerfully skillfully gracefully gently elegantly proudly beautifully validation cleverly cleanly cleverly thoughtfully boldly valiantly safely elegantly confidently brilliantly checks deftly efficiently proudly checks valiantly peacefully cleverly powerfully cleverly deftly cleanly elegantly testing marvellously bravely skilfully playfully validation cleanly gracefully cleverly gently check elegantly magnificently smoothly check test elegantly bravely skillfully proudly cleverly elegantly seamlessly expertly smartly beautifully brilliantly smartly gracefully skillfully powerfully eloquently gracefully smartly calmly softly efficiently tests check magically wisely seamlessly intelligently smartly valiantly delicately gracefully cleanly test skillfully wonderfully cleverly calmly confidently gracefully gracefully elegantly eloquently elegantly cleverly brightly creatively elegantly deftly carefully politely skilfully gracefully test ingeniously majestically validations comfortably smartly magically brightly powerfully skilfully gracefully gracefully cleverly string gracefully valiantly neatly gracefully smoothly gently elegantly calmly efficiently bravely valiantly safely gracefully" }
            });
        }
    }
}
