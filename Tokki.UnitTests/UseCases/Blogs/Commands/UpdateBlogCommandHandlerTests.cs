using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Blogs.Commands
{
    public class UpdateBlogCommandHandlerTests : BlogTestBase
    {
        private readonly UpdateBlogCommandHandler _updateHandler;

        private readonly Mock<ILogger<UpdateBlogCommandHandler>> _mockUpdateLogger;

        public UpdateBlogCommandHandlerTests()
        {
            _mockUpdateLogger = new Mock<ILogger<UpdateBlogCommandHandler>>();

            _updateHandler = new UpdateBlogCommandHandler(
                _mockRepo.Object,
                _mockUpdateLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_BlogNotFound()
        {
            var command = BlogTestData.GetValidUpdateBlogCommand("id-ao");
            _mockRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync((Blog?)null);

            var result = await _updateHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NewCategoryDoesNotExist()
        {
            var command = BlogTestData.GetValidUpdateBlogCommand("blog-1");
            command.CategoryId = "cate-khong-ton-tai";

            var oldBlog = new Blog { Id = "blog-1", CategoryId = "cate-cu" };

            _mockRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(oldBlog);
            _mockRepo.Setup(x => x.CategoryExistsAsync(command.CategoryId)).ReturnsAsync(false);

            var result = await _updateHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Category.NotFound");
        }

        [Fact]
        public async Task Handle_Should_UpdateAllFields_When_InputIsValid()
        {
            var command = BlogTestData.GetValidUpdateBlogCommand("blog-update");

            var oldBlog = new Blog
            {
                Id = "blog-update",
                Title = "Old Title",
                CategoryId = "cate-cu",
                Tags = new List<Tag> { new Tag { Name = "old-tag" } }
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(oldBlog);
            _mockRepo.Setup(x => x.CategoryExistsAsync(command.CategoryId)).ReturnsAsync(true);

            var newTagsEntities = new List<Tag>
            {
                new Tag { Name = "tag-moi-1" },
                new Tag { Name = "tag-moi-2" }
            };
            _mockRepo.Setup(x => x.GetOrCreateTagsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(newTagsEntities);

            var result = await _updateHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            oldBlog.Tags.Should().HaveCount(2);
            oldBlog.Tags.Should().Contain(t => t.Name == "tag-moi-1");

            oldBlog.Title.Should().Be(command.Title);
            oldBlog.CategoryId.Should().Be(command.CategoryId);
            oldBlog.Slug.Should().Contain("slug-tu-nhap");

            _mockRepo.Verify(x => x.UpdateAsync(oldBlog), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}