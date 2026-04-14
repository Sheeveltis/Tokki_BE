using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Passages.Queries.GetPassages;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Passages.Queries
{
    public class GetPassagesQueryHandlerTests : PassageTestBase
    {
        private readonly GetPassagesQueryHandler _handler;

        public GetPassagesQueryHandlerTests()
        {
            _handler = new GetPassagesQueryHandler(_mockPassageRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_MapPagedResult()
        {
            // Arrange
            var query = PassageTestData.GetPassagesQuery(
                pageNumber: 2,
                pageSize: 5,
                searchTerm: "abc",
                mediaType: PassageMediaType.Text,
                status: null);

            var p1 = PassageTestData.BuildPassage(passageId: "p1", title: "T1", mediaType: PassageMediaType.Text, status: PassageStatus.Active, content: "c1");
            var p2 = PassageTestData.BuildPassage(passageId: "p2", title: "T2", mediaType: PassageMediaType.Image, status: PassageStatus.Hidden, imageUrl: "img");

            var items = new List<Tokki.Domain.Entities.Passage> { p1, p2 };
            const int totalCount = 12;

            _mockPassageRepo
                .Setup(x => x.GetPagedAsync(
                    query.PageNumber,
                    query.PageSize,
                    query.SearchTerm,
                    query.MediaType,
                    query.Status,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((items, totalCount));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();

            result.Data.TotalCount.Should().Be(totalCount);
            result.Data.PageNumber.Should().Be(query.PageNumber);
            result.Data.PageSize.Should().Be(query.PageSize);

            result.Data.Items.Should().HaveCount(2);
            result.Data.Items[0].PassageId.Should().Be("p1");
            result.Data.Items[0].Title.Should().Be("T1");
            result.Data.Items[1].PassageId.Should().Be("p2");
            result.Data.Items[1].MediaType.Should().Be(PassageMediaType.Image);

            result.Message.Should().Be($"Found {totalCount} paragraphs.");

            _mockPassageRepo.Verify(x => x.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.SearchTerm,
                query.MediaType,
                query.Status,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
