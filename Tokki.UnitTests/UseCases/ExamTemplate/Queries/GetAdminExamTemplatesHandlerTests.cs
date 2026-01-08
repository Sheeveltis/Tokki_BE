using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Queries
{
    public class GetAdminExamTemplatesHandlerTests : ExamTemplateTestBase
    {
        private readonly GetAdminExamTemplatesQueryHandler _handler;

        public GetAdminExamTemplatesHandlerTests()
        {
            _handler = new GetAdminExamTemplatesQueryHandler(_mockExamTemplateRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult_When_Called()
        {
            var query = new GetAdminExamTemplatesQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "Test",
                Status = ExamTemplateStatus.Draft
            };

            var items = new List<ExamTemplate>
            {
                ExamTemplateTestData.GetDraftTemplate("T1"),
                ExamTemplateTestData.GetDraftTemplate("T2")
            };
            var totalCount = 20;

            _mockExamTemplateRepo.Setup(x => x.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.SearchTerm,
                query.Status,
                CancellationToken.None,
                query.Type
            )).ReturnsAsync((items, totalCount));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(totalCount);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);

            result.Data.Items.First().ExamTemplateId.Should().Be("T1");

            _mockExamTemplateRepo.Verify(x => x.GetPagedAsync(
                query.PageNumber, query.PageSize, query.SearchTerm, query.Status, CancellationToken.None, query.Type
            ), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmpty_When_NoDataFound()
        {
            var query = new GetAdminExamTemplatesQuery();

            _mockExamTemplateRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ExamTemplateStatus?>(), CancellationToken.None, It.IsAny<ExamType?>()
            )).ReturnsAsync((new List<ExamTemplate>(), 0));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}