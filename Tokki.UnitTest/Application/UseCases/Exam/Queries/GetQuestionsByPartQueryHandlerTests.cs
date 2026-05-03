using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Queries
{
    public class GetQuestionsByPartQueryHandlerTests
    {
        private readonly Mock<ITemplatePartRepository> _partMock = new();
        private readonly Mock<IQuestionBankRepository> _qbMock = new();

        private GetQuestionsByPartQueryHandler CreateHandler()
        {
            return new GetQuestionsByPartQueryHandler(_partMock.Object, _qbMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_01 | A | Template Part Missing -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TemplatePartNotFound_ShouldReturn404()
        {
            _partMock.Setup(x => x.GetByIdAsync("fake", It.IsAny<CancellationToken>())).ReturnsAsync((TemplatePart?)null);
            var handler = CreateHandler();
            
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "fake" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_01",
                Description = "Unmapped Template returns error immediately",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_02 | N | Pagination and Request Parameters mapped accurately
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ParametersMappedToRepo_ShouldSucceed()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart { QuestionTypeId = "Q1" });
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync("Q1", 2, 10, "Search", It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank>(), 0));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 2, PageSize = 10, SearchTerm = "Search" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_02",
                Description = "Verify input elements dynamically injected cleanly and calls valid repository filter",
                ExpectedResult = "Valid mapping true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Params verified in Setup" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_03 | N | Fallback Null SkillMediaType mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SkillWriting_ShouldFallbackToImageMediaType()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var qb = new QuestionBank { QuestionBankId = "qb1", QuestionType = new QuestionType { Skill = QuestionSkill.Writing } };
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().MediaType.Should().Be("Image");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_03",
                Description = "Writing maps exclusively directly to default Image wrapper securely logic",
                ExpectedResult = "MediaType = Image fallback",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill is Writing" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_04 | N | Audio Skill MediaType mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SkillListening_ShouldMapToAudioMediaType()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var qb = new QuestionBank { QuestionBankId = "qb1", QuestionType = new QuestionType { Skill = QuestionSkill.Listening } };
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().MediaType.Should().Be("Audio");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_04",
                Description = "Listening skill explicitly directs mapping Audio wrapper efficiently logic",
                ExpectedResult = "MediaType = Audio mapped",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill is Listening" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_05 | N | Options correctly mapped and ordered by KeyOption
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OptionsMapping_ShouldStrictlyOrderAscending()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var qb = new QuestionBank { QuestionBankId = "qb1" };
            qb.QuestionOptions.Add(new QuestionOption { KeyOption = "B" });
            qb.QuestionOptions.Add(new QuestionOption { KeyOption = "A" });

            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().Options.Should().HaveCount(2);
            result.Data.Items.First().Options.First().KeyOption.Should().Be("A"); // Ordered!

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_05",
                Description = "Output data format validates explicitly returning sorted data on OptionKeys dynamically",
                ExpectedResult = "Ascending Option format true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unordered internal database array sorted correctly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_06 | N | Passages Null Check Trapping Exception Safety
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PassagesNull_MapsToEmptyWithoutError()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var qb = new QuestionBank { QuestionBankId = "qb1", Passage = null };
            
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().PassageContent.Should().BeNull();
            result.Data.Items.First().PassageMediaType.Should().BeNull();

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_06",
                Description = "Optional missing sub-items strictly fall back to Safe JSON mapped objects handling strings securely",
                ExpectedResult = "Object resolves passage effectively",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Passage element is null" }
            });
        }
        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_07 | N | Passage Not Null Maps Content Securely
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PassageNotNull_MapsCompletePassageSafely()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var passage = new Passage { Content = "Passage Content", ImageUrl = "http://p.img", AudioUrl = "http://p.aud", MediaType = PassageMediaType.Image };
            var qb = new QuestionBank { QuestionBankId = "qb1", Passage = passage };
            
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().PassageContent.Should().Be("Passage Content");
            result.Data.Items.First().PassageMediaType.Should().Be("Image");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_07",
                Description = "Fully populated passage object successfully mapped cleanly correctly dynamically",
                ExpectedResult = "Passage object resolves properties successfully",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Passage element is fully populated" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionsByPartQueryHandler_08 | N | Skill is Null Maps Object Image Formats
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SkillIsNull_ShouldMapToImageDefault()
        {
            _partMock.Setup(x => x.GetByIdAsync("tp1", It.IsAny<CancellationToken>())).ReturnsAsync(new TemplatePart());
            
            var qb = new QuestionBank { QuestionBankId = "qb1", QuestionType = null };
            _qbMock.Setup(x => x.GetAvailableQuestionsByTypeAsync(null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<QuestionBank> { qb }, 1));
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetQuestionsByPartQuery { TemplatePartId = "tp1", PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().MediaType.Should().Be("Image");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "GetQuestionsByPartQueryHandler",
                TestCaseID = "GetQuestionsByPartQueryHandler_08",
                Description = "Null skill correctly handles logic implicitly mapping Image natively seamlessly securely",
                ExpectedResult = "MediaType = Image fallback automatically",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill is null" }
            });
        }
    }
}
