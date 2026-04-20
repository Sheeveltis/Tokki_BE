using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class UpdateBlogCommandHandlerTests
    {
        // -----------------------------------------------------------
        // FACTORY
        // -----------------------------------------------------------
        private static UpdateBlogCommandHandler CreateHandler(Mock<IBlogRepository>? repo = null)
        {
            repo ??= MockBlogRepository.GetMock();
            var logger = new Mock<ILogger<UpdateBlogCommandHandler>>();

            return new UpdateBlogCommandHandler(repo.Object, logger.Object);
        }

        // -----------------------------------------------------------
        // Update_Blog_01 | A | Blog Not Found ? 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new UpdateBlogCommand { Id = "MISSING-BLOG" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_01",
                Description       = "Attempt to update a non-existent blog",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // -----------------------------------------------------------
        // Update_Blog_02 | A | Category Changed and Invalid ? 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_InvalidCategoryChange_ShouldReturn404Category()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.CategoryId = "OLD-CAT";
            
            var mockRepo = MockBlogRepository.GetMock(new List<Blog> { blog });
            
            // Set up mock so ONLY"OLD-CAT" exists
            mockRepo.Setup(x => x.CategoryExistsAsync("INVALID-CAT")).ReturnsAsync(false);

            var command = new UpdateBlogCommand 
            { 
                Id = "BLOG-1", 
                CategoryId = "INVALID-CAT" // Changed category
            };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.CategoryNotFound);

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_02",
                Description       = "Change category to a non-existent category ID",
                ExpectedResult    = "Return 404 CategoryNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CategoryExistsAsync returns false", "Return 404" }
            });
        }

        // -----------------------------------------------------------
        // Update_Blog_03 | N | Valid Update without explicit Slug (uses Title) -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NoExplicitSlug_ShouldGenerateSlugFromTitle()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var command = new UpdateBlogCommand 
            { 
                Id = "BLOG-1", 
                Title = "New Title Format", 
                CategoryId = "VALID-CAT",
                Slug = "" // Empty slug
            };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify slug generation
            blog.Slug.Should().Be("new-title-format-BLOG-1");
            mockRepo.Verify(x => x.UpdateAsync(blog), Times.Once);

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_03",
                Description       = "Update without explicit slug, forces fallback to Title generation",
                ExpectedResult    = "Return 200, Slug matches newly generated title",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Slug is null/whitespace", "Generate slug from Title" }
            });
        }

        // -----------------------------------------------------------
        // Update_Blog_04 | N | Valid Update with explicit Slug -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ExplicitSlug_ShouldGenerateSlugFromProvidedSlug()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var command = new UpdateBlogCommand 
            { 
                Id = "BLOG-1", 
                Title = "Ignored Title For Slug", 
                CategoryId = "VALID-CAT",
                Slug = "Custom Slug Provided"
            };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            blog.Slug.Should().Be("custom-slug-provided-BLOG-1");

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_04",
                Description       = "Update with explicit custom slug",
                ExpectedResult    = "Return 200, Slug is derived from provided explicit string",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Slug is valid", "Generate slug from explicit Slug string" }
            });
        }

        // -----------------------------------------------------------
        // Update_Blog_05 | N | Tag list is cleared and regenerated
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_WithNewTags_ShouldClearAndAddTags()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.Tags = new List<Tag> { new Tag { Name = "OldTag" } };
            
            var mockRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var command = new UpdateBlogCommand 
            { 
                Id = "BLOG-1", 
                CategoryId = "VALID-CAT",
                Tags = new List<string> { "NewTag1", "NewTag2" }
            };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            // Should contain exactly 2 tags now (old one removed)
            blog.Tags.Should().HaveCount(2);

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_05",
                Description       = "Update tags with a new list",
                ExpectedResult    = "Old tags cleared, exactly matching new tag list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Tags list provided", "Clear and Repopulate Tags" }
            });
        }

        // -----------------------------------------------------------
        // Update_Blog_06 | A | Database Exception triggers 500 ServerError
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Blog> { blog });
            
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Mock Update Failure"));

            var command = new UpdateBlogCommand { Id = "BLOG-1", CategoryId = "VALID-CAT" };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Contain("L?i h? th?ng: Mock Update Failure");
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Blog - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Blog",
                TestCaseID        = "Update_Blog_06",
                Description       = "Simulate exception during Update save context",
                ExpectedResult    = "Return 500 ServerError with inner exception details",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception", "Return 500" }
            });
        }
    }
}
