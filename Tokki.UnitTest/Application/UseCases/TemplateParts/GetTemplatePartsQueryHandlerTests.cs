using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.TemplateParts.Queries.GetTemplateParts;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TemplateParts
{
    public class GetTemplatePartsQueryHandlerTests
    {
        private static Mock<ITemplatePartRepository> GetRepoMock(
            IEnumerable<TemplatePart>? items = null, int total = 0)
        {
            var m = new Mock<ITemplatePartRepository>();
            m.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(((IEnumerable<TemplatePart>)(items ?? new List<TemplatePart>()), total));
            return m;
        }

        private static GetTemplatePartsQueryHandler CreateHandler(Mock<ITemplatePartRepository>? repo = null)
            => new GetTemplatePartsQueryHandler((repo ?? GetRepoMock()).Object);

        private static GetTemplatePartsQuery MakeQuery(string? templateId = "T1", int page = 1, int size = 10)
            => new GetTemplatePartsQuery { ExamTemplateId = templateId, PageNumber = page, PageSize = size };

        private static List<TemplatePart> SampleParts() => new List<TemplatePart>
        {
            new TemplatePart { TemplatePartId = "TP-001", ExamTemplateId = "T1", Skill = QuestionSkill.Listening, QuestionFrom = 1,  QuestionTo = 10, PartTitle = "Part 1", Mark = 2, QuestionType = new QuestionType { Name = "MC" } },
            new TemplatePart { TemplatePartId = "TP-002", ExamTemplateId = "T1", Skill = QuestionSkill.Reading,   QuestionFrom = 11, QuestionTo = 20, PartTitle = "Part 2", Mark = 3, QuestionType = null! }
        };

        // TC-TP-GLIST-01 | N | Happy path: 2 parts returned → PagedResult Count=2
        [Fact]
        public async Task Handle_RepoReturnsData_ShouldReturnPagedResult()
        {
            var repo   = GetRepoMock(SampleParts(), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-01", Description = "2 parts returned → PagedResult Count=2, TotalCount=2", ExpectedResult = "IsSuccess=true, Items.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetPagedAsync returns 2 items" } });
        }

        // TC-TP-GLIST-02 | N | TemplatePartDto fields mapped correctly
        [Fact]
        public async Task Handle_ReturnsData_DtoFieldsMappedCorrectly()
        {
            var repo   = GetRepoMock(SampleParts(), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            var first  = result.Data!.Items[0];
            first.TemplatePartId.Should().Be("TP-001");
            first.Skill.Should().Be(QuestionSkill.Listening);
            first.QuestionFrom.Should().Be(1);
            first.Mark.Should().Be(2);
            first.QuestionTypeName.Should().Be("MC");
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-02", Description = "TemplatePartDto fields correctly mapped from entity", ExpectedResult = "Id='TP-001', Skill=Listening, From=1, Mark=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All DTO fields verified" } });
        }

        // TC-TP-GLIST-03 | N | QuestionType null → QuestionTypeName = 'Chưa phân loại'
        [Fact]
        public async Task Handle_PartHasNullQuestionType_QuestionTypeNameIsDefaultText()
        {
            var repo   = GetRepoMock(SampleParts(), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            var second = result.Data!.Items[1]; // TP-002 has null QuestionType
            second.QuestionTypeName.Should().Be("Chưa phân loại");
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-03", Description = "QuestionType=null → QuestionTypeName='Chưa phân loại'", ExpectedResult = "QuestionTypeName='Chưa phân loại'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Null nav property handled with default text" } });
        }

        // TC-TP-GLIST-04 | B | GetPagedAsync called with correct params
        [Fact]
        public async Task Handle_WithPaging_GetPagedCalledWithCorrectParams()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(MakeQuery(templateId: "T99", page: 2, size: 5), CancellationToken.None);
            repo.Verify(x => x.GetPagedAsync(2, 5, "T99", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-04", Description = "GetPagedAsync called with Page=2, Size=5, TemplateId='T99'", ExpectedResult = "Times.Once with correct params", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All params forwarded to repo" } });
        }

        // TC-TP-GLIST-05 | N | Empty list → 200 with empty PagedResult
        [Fact]
        public async Task Handle_NoParts_ShouldReturn200WithEmptyPage()
        {
            var result = await CreateHandler(GetRepoMock(new List<TemplatePart>(), 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-05", Description = "No parts → 200 with empty paged result", ExpectedResult = "IsSuccess=true, Items=[], TotalCount=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No template parts exist" } });
        }

        // TC-TP-GLIST-06 | N | Paged metadata (PageNumber, PageSize, TotalCount) correct
        [Fact]
        public async Task Handle_WithTotalCount50_PagingMetadataCorrect()
        {
            var repo   = GetRepoMock(SampleParts(), total: 50);
            var result = await CreateHandler(repo).Handle(MakeQuery(page: 3, size: 10), CancellationToken.None);
            result.Data!.PageNumber.Should().Be(3);
            result.Data.PageSize.Should().Be(10);
            result.Data.TotalCount.Should().Be(50);
            QACollector.LogTestCase("TemplatePart - Get List", new TestCaseDetail { FunctionGroup = "GetTemplateParts", TestCaseID = "TC-TP-GLIST-06", Description = "Paging metadata: Page=3, Size=10, TotalCount=50", ExpectedResult = "Metadata correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TotalCount=50, page 3 of 10" } });
        }
    }
}
