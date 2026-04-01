using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.Blogs.Commands
{
    public class DeleteBlogCommandHandlerTests : BlogTestBase
    {
        private readonly DeleteBlogCommandHandler _deleteHandler;

        public DeleteBlogCommandHandlerTests()
        {
            _deleteHandler = new DeleteBlogCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_BlogNotFound()
        {
            var command = new DeleteBlogCommand { Id = "id-khong-ton-tai" };

            _mockRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync((Blog?)null);

            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            _mockRepo.Verify(x => x.DeleteAsync(It.IsAny<Blog>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ExecuteSoftDelete_When_BlogExists()
        {
            var command = new DeleteBlogCommand { Id = "blog-bi-xoa" };
            var existingBlog = new Blog { Id = "blog-bi-xoa", Title = "Old Blog" };

            _mockRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(existingBlog);

            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _mockRepo.Verify(x => x.DeleteAsync(existingBlog), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}