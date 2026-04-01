using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Blogs.Queries;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Blogs.Queries
{
    public class GetBlogByIdQueryHandlerTests : BlogTestBase
    {
        private readonly GetBlogByIdQueryHandler _queryHandler;

        public GetBlogByIdQueryHandlerTests()
        {
            _queryHandler = new GetBlogByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_BlogNotFound()
        {
            var query = new GetBlogByIdQuery { Id = "id-khong-ton-tai" };

            _mockRepo.Setup(x => x.GetByIdAsync(query.Id))
                     .ReturnsAsync((Blog?)null);

            var result = await _queryHandler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Blog.NotFound");
        }

        [Fact]
        public async Task Handle_Should_ReturnBlogDetailDTO_When_BlogExists()
        {
            var query = new GetBlogByIdQuery { Id = "blog-detail-1" };
            var fakeBlog = BlogTestData.GetFakeBlogDetail();

            _mockRepo.Setup(x => x.GetByIdAsync(query.Id))
                     .ReturnsAsync(fakeBlog);

            var result = await _queryHandler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            var dto = result.Data;

            dto.Id.Should().Be(fakeBlog.Id);
            dto.Title.Should().Be(fakeBlog.Title);

            dto.Content.Should().Be(fakeBlog.Content);

            dto.CategoryName.Should().Be("Culture");
            dto.Tags.Should().HaveCount(2);
            dto.Tags.Should().Contain("Tag1");

            dto.Status.Should().NotBeNullOrEmpty(); 
        }
    }
}