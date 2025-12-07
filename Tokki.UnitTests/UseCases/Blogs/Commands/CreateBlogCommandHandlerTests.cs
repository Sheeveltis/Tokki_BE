using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;    
using Tokki.UnitTests.Common.TestData; 
using Xunit;

namespace Tokki.UnitTests.Features.Blogs.Commands
{
    public class CreateBlogCommandHandlerTests : BlogTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CategoryDoesNotExist()
        {
            var command = BlogTestData.GetInvalidCategoryCommand();

            _mockRepo.Setup(x => x.CategoryExistsAsync(command.CategoryId))
                     .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Category.NotFound");

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<Blog>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            var command = BlogTestData.GetValidCreateBlogCommand();

            _mockRepo.Setup(x => x.CategoryExistsAsync(command.CategoryId)).ReturnsAsync(true);

            _mockRepo.Setup(x => x.GetOrCreateTagsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(BlogTestData.GetFakeTags());

            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("blog-new-1");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("blog-new-1");

            _mockRepo.Verify(x => x.AddAsync(It.Is<Blog>(b =>
                b.Id == "blog-new-1" &&
                b.Title == command.Title && 
                b.CategoryId == command.CategoryId &&
                b.Tags.Count == 2
            )), Times.Once);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DatabaseErrorOccurs()
        {
            var command = BlogTestData.GetValidCreateBlogCommand();

            _mockRepo.Setup(x => x.CategoryExistsAsync(command.CategoryId)).ReturnsAsync(true);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new System.Exception("DB Connection Failed"));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == "App.ServerError");
        }
    }
}