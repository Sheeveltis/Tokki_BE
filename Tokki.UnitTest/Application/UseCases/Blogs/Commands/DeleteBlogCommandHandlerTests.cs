using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class DeleteBlogCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static DeleteBlogCommandHandler CreateHandler(Mock<IBlogRepository>? repo = null)
        {
            return new DeleteBlogCommandHandler((repo ?? MockBlogRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-01 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new DeleteBlogCommand { Id = "MISSING" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-01",
                Description       = "Attempt to delete a non-existent blog",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-02 | N | Valid Deletion → Returns 200 Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldDeleteAndReturn200()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var command = new DeleteBlogCommand { Id = "BLOG-1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-02",
                Description       = "Provide valid existing blog ID for deletion",
                ExpectedResult    = "Return 200 Success",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Blog ID", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-03 | N | DeleteAsync Verification
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallDeleteAsyncOnRepository()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var command = new DeleteBlogCommand { Id = "BLOG-1" };
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.DeleteAsync(blog), Times.Once);

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-03",
                Description       = "Verify that DeleteAsync is invoked exactly once",
                ExpectedResult    = "DeleteAsync called x1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository method call verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-04 | N | SaveChangesAsync Verification
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallSaveChangesAsync()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var command = new DeleteBlogCommand { Id = "BLOG-1" };
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-04",
                Description       = "Verify that SaveChangesAsync is invoked after deletion",
                ExpectedResult    = "SaveChangesAsync called x1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Database commit verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-05 | N | Database Exception → Returns 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500ServerError()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<Tokki.Domain.Entities.Blog>()))
                    .ThrowsAsync(new Exception("DB Failure"));

            var command = new DeleteBlogCommand { Id = "BLOG-1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-05",
                Description       = "Simulate database exception during DeleteAsync",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository throws Exception", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-DB-06 | B | Empty id string
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyBlogId_ShouldReturn404()
        {
            var command = new DeleteBlogCommand { Id = "" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Blog - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Blog",
                TestCaseID        = "TC-DB-06",
                Description       = "Provide empty string as Blog ID",
                ExpectedResult    = "Return 404 BlogNotFound due to failed lookup",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty ID yields null lookup", "Return 404" }
            });
        }
    }
}
