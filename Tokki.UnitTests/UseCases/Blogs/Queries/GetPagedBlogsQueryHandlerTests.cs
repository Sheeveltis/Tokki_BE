using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Blogs.Queries
{
    public class GetPagedBlogsQueryHandlerTests : BlogTestBase
    {
        private readonly GetPagedBlogsQueryHandler _queryHandler;

        public GetPagedBlogsQueryHandlerTests()
        {
            _queryHandler = new GetPagedBlogsQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_MapDataCorrectly_When_BlogsExist()
        {
            var query = new GetPagedBlogsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Status = BlogStatus.Published
            };

            var fakeEntities = BlogTestData.GetFakeBlogEntities();
            var pagedResultEntity = new PagedResult<Blog>(fakeEntities, 1, 1, 10);

            _mockRepo.Setup(x => x.GetPagedAsync(
                query.PageNumber,
                query.PageSize,
                query.CategoryId,
                query.Status,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResultEntity);

            var result = await _queryHandler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);

            var item = result.Data.Items.First();

            item.Title.Should().Be("Standard Test Blog");
            item.CategoryName.Should().Be("Culture");
            item.Tags.Should().Contain("Tag1");
            item.Tags.Should().HaveCount(2);
            item.Status.Should().NotBeNullOrEmpty(); 
        }

        [Fact]
        public async Task Handle_Should_HandleNullCategory_Correctly()
        {
            var query = new GetPagedBlogsQuery();

            var fakeEntities = BlogTestData.GetFakeBlogEntitiesWithNullCategory();
            var pagedResultEntity = new PagedResult<Blog>(fakeEntities, 1, 1, 10);

            _mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(pagedResultEntity);

            var result = await _queryHandler.Handle(query, CancellationToken.None);

            var item = result.Data.Items.First();

            item.CategoryName.Should().Be("Not determined");
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoDataFound()
        {
            var query = new GetPagedBlogsQuery();
            var emptyResult = new PagedResult<Blog>(new List<Blog>(), 0, 1, 10);

            _mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(emptyResult);

            var result = await _queryHandler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}