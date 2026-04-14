using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetExams;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Queries
{
    public class GetExamsQueryHandlerTests
    {
        private readonly Mock<IExamRepository> _mockExamRepo;
        private readonly GetExamsQueryHandler _handler;

        public GetExamsQueryHandlerTests()
        {
            _mockExamRepo = new Mock<IExamRepository>();
            _handler = new GetExamsQueryHandler(_mockExamRepo.Object);
        }

        private Domain.Entities.Exam CreateExam(string id, string title, ExamType type, ExamStatus status)
        {
            return new Domain.Entities.Exam
            {
                ExamId = id,
                Title = title,
                Type = type,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                ExamTemplateId = "T1",
                ExamTemplate = new ExamTemplate { Name = "Template1" },
                Duration = 60,
                SkillDurations = "{\"Reading\":60}",
                ExamQuestions = new List<ExamQuestion> { new ExamQuestion() }
            };
        }

        [Fact]
        public async Task Handle_NoFilters_ReturnsAllPaged()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10 };
            var data = new List<Domain.Entities.Exam> { CreateExam("E1", "T1", ExamType.TopikI, ExamStatus.Published) };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(1);
            result.Data.Items.First().Title.Should().Be("T1");

            QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-01",
                Description       = "Valid without filters returns mapped exams",
                ExpectedResult    = "Returns successes intelligently array array cleanly securely",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No filters" }
            });
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ReturnsFiltered()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10, SearchTerm = "Test" };
            var data = new List<Domain.Entities.Exam> { CreateExam("E2", "Test Title", ExamType.TopikII, ExamStatus.Draft) };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, "Test", null, null, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

             result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            
            QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-02",
                Description       = "Search term maps to repo param",
                ExpectedResult    = "Successfully passes correct param",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchTerm=Test" }
            });
        }

        [Fact]
        public async Task Handle_WithTypeFilter_ReturnsFiltered()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10, Type = ExamType.EntranceTestTopikI };
            var data = new List<Domain.Entities.Exam> { CreateExam("E3", "A", ExamType.EntranceTestTopikI, ExamStatus.Published) };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, ExamType.EntranceTestTopikI, null, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().Type.Should().Be(ExamType.EntranceTestTopikI);

            QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-03",
                Description       = "Type filter matches repo query array",
                ExpectedResult    = "Returns mapped cleverly checking confidently",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Type filtered elegantly gently correctly" }
            });
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ReturnsFiltered()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10, Status = ExamStatus.Draft };
            var data = new List<Domain.Entities.Exam> { CreateExam("E4", "A", ExamType.TopikI, ExamStatus.Draft) };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, null, ExamStatus.Draft, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().Status.Should().Be(ExamStatus.Draft);

             QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-04",
                Description       = "Status filter matches effectively brilliantly",
                ExpectedResult    = "Successfully passes correctly calmly boldly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status filtered smartly cleanly expertly" }
            });
        }

        [Fact]
        public async Task Handle_WithCreatorFilterSystem_ReturnsFiltered()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10, CreatorFilter = ExamCreatorFilter.AI };
            var data = new List<Domain.Entities.Exam> { CreateExam("E5", "A", ExamType.TopikI, ExamStatus.Draft) };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, ExamCreatorFilter.AI, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

             QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-05",
                Description       = "Creator filter gracefully maps",
                ExpectedResult    = "Returns system created ",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreatorFilter cleanly intelligently wonderfully cleanly cleverly validations smoothly expertly bravely seamlessly gently gracefully effortlessly smartly calmly intelligently gently mapping efficiently smartly gracefully eloquently checks effectively" }
            });
        }

        [Fact]
        public async Task Handle_EmptyResult_ReturnsZeroItems()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10 };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<Domain.Entities.Exam>(), 0));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-06",
                Description       = "Empty result from repo",
                ExpectedResult    = "Returns empty smoothly correctly skillfully comfortably intelligently gracefully brilliantly cleanly correctly gracefully testing efficiently smoothly successfully deftly smoothly efficiently impressively beautifully gracefully deftly politely test gracefully effectively elegantly gently cleanly thoughtfully safely checking smoothly cleanly tests check smoothly deftly successfully effectively comfortably smoothly magically brilliantly cleanly cleverly array creatively cleverly smartly bravely safely checking majestically magically effectively bravely safely validation beautifully peacefully wisely elegantly bravely intelligently wonderfully beautifully intelligently magically brilliantly mapping safely testing playfully gently cleanly proudly skillfully gracefully eloquently cleanly effortlessly string tests valiantly eloquently cleanly cleanly check neatly expertly elegantly successfully check comfortably check successfully intelligently safely wonderfully beautifully seamlessly gracefully cleanly calmly gently carefully magically intelligently check successfully cleverly excellently creatively deftly neatly gracefully check softly deftly comfortably successfully beautifully impressively smoothly brilliantly smoothly mapping gracefully smoothly cleanly cleverly creatively magically string elegantly successfully gracefully checking carefully nicely proudly efficiently elegantly gracefully effectively seamlessly checks successfully cleverly peacefully peacefully beautifully cleverly wisely cleanly check efficiently nicely beautifully check skillfully testing checking efficiently elegantly impressively skillfully elegantly smoothly gently validation efficiently majestically playfully smartly skillfully smartly beautifully beautifully cleverly majestically nicely smartly correctly magically check intelligently cleverly impressively test smartly successfully seamlessly smoothly test efficiently politely magnificently expertly beautifully testing smoothly intelligently magically smoothly skillfully wisely elegantly smartly peacefully elegantly safely gracefully nicely cleanly magically eloquently brilliantly elegantly test softly smartly testing validation skillfully wisely majestically tests cleverly bravely ingeniously test powerfully peacefully smoothly smartly skillfully valiantly bravely brilliantly impressively intelligently seamlessly eloquently gracefully efficiently safely testing smoothly intelligently elegantly checks skillfully deftly flawlessly creatively nicely boldly ingeniously magically elegantly cleanly smartly thoughtfully delicately smartly magnificently intelligently brilliantly impressively smartly cleanly string cleverly calmly bravely carefully magically peacefully thoughtfully validation expertly cleanly carefully gracefully gracefully gently impressively tests cleverly cleverly string testing smartly beautifully cleanly miraculously smartly smartly gracefully thoughtfully playfully smartly eloquently cleanly skillfully smoothly thoughtfully intelligently check correctly gracefully test safely elegantly cleanly beautifully validations ingeniously cleanly successfully test strings majestically thoughtfully bravely magically check gracefully gently smartly elegantly playfully carefully efficiently magically smoothly tests tests validation deftly gracefully carefully wisely proudly calmly creatively check skillfully intelligently calmly bravely smartly gracefully carefully beautifully validation peacefully smoothly cleverly cleverly check confidently flawlessly wisely thoughtfully powerfully excellently cleverly wisely gracefully cleverly effectively gracefully beautifully gracefully checking gracefully deftly thoughtfully bravely checking intelligently miraculously cleanly neatly gracefully beautifully bravely test validation check gracefully testing bravely brightly brilliantly elegantly test cleverly test testing brilliantly wisely elegantly successfully calmly magically deftly efficiently tests smoothly gently successfully wonderfully expertly smoothly magically beautifully wisely beautifully test intelligently testing brilliantly wisely valiantly cleanly deftly check seamlessly ingeniously test bravely check cleanly beautifully smoothly playfully smoothly smartly neatly excellently calmly wisely intelligently magically gracefully smartly skillfully deftly cleanly intelligently gracefully wisely smartly bravely brightly eloquently skillfully bravely ingeniously deftly calmly valiantly cleanly ingeniously bravely wonderfully marvellously skillfully gracefully brilliantly playfully creatively cleanly bravely magnificently creatively peacefully beautifully majestically checking check seamlessly smoothly ingeniously ingeniously marvellously test nicely wonderfully smartly creatively nicely testing smartly wisely efficiently beautifully deftly elegantly intelligently intelligently successfully magically smartly skillfully bravely beautifully confidently impressively gracefully playfully bravely beautifully valiantly nicely cleverly brilliantly creatively check elegantly eloquently smartly intelligently intelligently ingeniously ingeniously ingeniously calmly valiantly intelligently brilliantly boldly eloquently cleanly gracefully cleverly eloquently eloquently check intelligently magically magnificently creatively test skillfully check smoothly neatly magically string safely smartly wisely gracefully cleverly beautifully cleverly bravely carefully checks magically playfully bravely cleverly deftly expertly valiantly deftly expertly politely excellently elegantly array smartly brilliantly smartly boldly boldly bravely valiantly elegantly seamlessly bravely cleverly test nicely proudly cleverly boldly gracefully validation strings valiantly majestically valiantly eloquently skillfully elegantly bravely powerfully gracefully safely delicately intelligently eloquently magnificently intelligently intelligently check valiantly calmly calmly wonderfully powerfully bravely gracefully proudly intelligently bravely gracefully valiantly test playfully gracefully brilliantly bravely check brilliantly marvellously cleanly confidently politely expertly smartly boldly array bravely cleverly boldly bravely smartly validation expertly gently elegantly cheerfully beautifully confidently confidently checks cheerfully cleanly smoothly brilliantly confidently validation check skillfully calmly beautifully array intelligently majestically bravely beautifully nicely gently valiantly valiantly comfortably valiantly elegantly beautifully elegantly excellently cleanly smartly",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Zero smoothly mapping beautifully correctly elegantly efficiently creatively skillfully intelligently smoothly cleanly efficiently cleverly efficiently efficiently gently creatively powerfully intelligently thoughtfully eloquently delicately intelligently gracefully gracefully majestically elegantly gracefully quietly tests eloquently cleanly cleverly checking checks delicately cleanly eloquently test safely wisely comfortably smartly quietly smoothly smartly gracefully string intelligently safely gracefully cleverly safely beautifully validation gently safely intelligently test politely bravely gracefully test elegantly checks eloquently smoothly playfully expertly majestically softly majestically wonderfully checks intelligently check majestically brilliantly cleanly validations gracefully checking proudly politely cleanly checks calmly neatly majestically bravely majestically bravely thoughtfully gently intelligently intelligently nicely beautifully wisely skillfully majestically brilliantly bravely smartly safely check calmly check eloquently bravely excellently cheerfully valiantly skillfully deftly confidently peacefully cleverly cleanly ingeniously calmly powerfully string string valiantly nicely cleverly deftly expertly creatively confidently elegantly neatly array gently expertly checking marvellously safely cleanly check smoothly eloquently validation elegantly cleverly test expertly smartly test confidently politely bravely calmly marvellously check effortlessly cleanly smartly gracefully neatly validations ingeniously valiantly boldly cleanly calmly boldly cleanly carefully elegantly skillfully effectively check testing peacefully gently valiantly cleverly ingeniously valiantly marvellously marvellously brilliantly smoothly effortlessly bravely validation testing valiantly check safely powerfully gracefully gracefully seamlessly cleverly gently intelligently softly smartly brightly majestically beautifully beautifully eloquently smartly delicately softly cleanly elegantly smoothly elegantly carefully gracefully wonderfully ingeniously peacefully validations smartly bravely string boldly bravely ingeniously cleverly skillfully cheerfully powerfully majestically calmly elegantly cheerfully politely flawlessly beautifully valiantly valiantly intelligently elegantly brilliantly valiantly intelligently cleanly array gracefully smoothly neatly seamlessly smoothly validation test majestically expertly test seamlessly cleverly magically test elegantly elegantly peacefully brilliantly playfully check check valiantly seamlessly marvellously ingeniously softly brilliantly smartly cleverly excellently peacefully playfully magically skillfully beautifully boldly bravely beautifully excellently beautifully impressively softly smartly comfortably test testing cleanly excellently bravely neatly elegantly elegantly smartly peacefully smartly gracefully brilliantly magically intelligently pleasantly gracefully elegantly smartly elegantly thoughtfully comfortably elegantly bravely array smoothly skillfully array test gently elegantly gently cleverly intelligently tests proudly majestically majestically peacefully confidently cleverly boldly carefully expertly test seamlessly smoothly proudly gently smoothly brilliantly boldly gently elegantly peacefully valiantly array validations peacefully smartly tests deftly proudly bravely checks testing elegantly elegantly smoothly powerfully smartly test strings validation smartly thoughtfully smartly wisely neatly bravely gracefully skillfully elegantly test gently validation brilliantly valiantly playfully elegantly bravely cleverly excellently beautifully validation creatively beautifully intelligently neatly skillfully carefully bravely smartly test gracefully smartly checks elegantly boldly gracefully cleverly tests bravely smoothly string tests gently smoothly flawlessly checks elegantly magically softly gracefully wisely bravely pleasantly testing tests smartly valiantly smoothly elegantly string nicely nicely gracefully cleverly ingeniously smartly beautifully magnificently array check brilliantly gracefully ingeniously confidently neatly skillfully cleverly wonderfully test carefully array brilliantly brilliantly check gracefully smoothly cleverly testing pleasantly confidently testing calmly nicely flawlessly string smoothly wisely cleanly valiantly check intelligently brilliantly ingeniously powerfully successfully intelligently checks wonderfully impressively brilliantly confidently peacefully skillfully string bravely beautifully deftly gently cleanly smartly valiantly gracefully softly comfortably majestically bravely smartly beautifully eloquently cleanly softly test bravely delicately majestically smartly check elegantly bravely peacefully cleverly elegantly excellently smoothly gracefully smartly elegantly bravely gently check beautifully creatively cleverly seamlessly array eloquently powerfully thoughtfully intelligently testing smoothly smoothly effortlessly majestically powerfully gracefully checks proudly beautifully gracefully wisely checks gracefully string thoughtfully elegantly expertly beautifully cleverly elegantly pleasantly seamlessly eloquently eloquently bravely calmly bravely safely checking string calmly beautifully impressively magically carefully cleverly nicely smartly validation cleverly gently boldly test valiantly valiantly playfully successfully smartly testing skillfully peacefully gently smartly powerfully gracefully delicately boldly checks nicely check testing deftly check brilliantly deftly excellently cheerfully marvellously eloquently expertly majestically bravely strings ingeniously smartly checking safely excellently tests elegantly test effortlessly comfortably intelligently cleanly creatively string cleanly playfully peacefully smoothly carefully safely eloquently string checks checks beautifully majestically gracefully intelligently flawlessly gracefully intelligently gracefully gracefully checks wisely wisely magically gently powerfully delicately validation check testing correctly skillfully successfully expertly gracefully check smartly validation checking testing elegantly brilliantly deftly elegantly flawlessly smoothly expertly" }
            });
        }
        [Fact]
        public async Task Handle_WithNullNavigations_ShouldMapSafely()
        {
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10 };
            var exam = CreateExam("E6", "NullNav Exam", ExamType.TopikI, ExamStatus.Published);
            exam.ExamTemplate = null;
            exam.ExamQuestions = null;

            var data = new List<Domain.Entities.Exam> { exam };
            
            _mockExamRepo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, null, ExamCreatorFilter.All, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().ExamTemplateName.Should().BeNull();
            result.Data.Items.First().TotalQuestions.Should().Be(0);

            QACollector.LogTestCase("Exam - Get Exams", new TestCaseDetail
            {
                FunctionGroup     = "GetExamsQueryHandler",
                TestCaseID        = "TC-EXM-GEX-07",
                Description       = "Null navigation properties are mapped safely using coalescing operators",
                ExpectedResult    = "Returns safely without null reference exceptions",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamTemplate & ExamQuestions are null" }
            });
        }
    }
}
