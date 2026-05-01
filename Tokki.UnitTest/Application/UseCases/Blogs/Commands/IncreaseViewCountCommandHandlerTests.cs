using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class IncreaseViewCountCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static IncreaseViewCountCommandHandler CreateHandler(Mock<IBlogRepository>? repo = null)
        {
            return new IncreaseViewCountCommandHandler((repo ?? MockBlogRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_01 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new IncreaseViewCountCommand { BlogId = "MISSING" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_01",
                Description       = "Provide an ID that does not exist",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IncreaseViewCountAsync returns false", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_02 | N | Valid Update → Returns 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200Success()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var command = new IncreaseViewCountCommand { BlogId = "BLOG-1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Tăng view thành công.");

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_02",
                Description       = "Valid blog ID provided",
                ExpectedResult    = "Return 200 and successful message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IncreaseViewCountAsync returns true", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_03 | B | Empty String ID → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyString_ShouldReturn404()
        {
            var command = new IncreaseViewCountCommand { BlogId = "" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_03",
                Description       = "Empty string passed as Blog ID",
                ExpectedResult    = "Lookup fails, return 404",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty ID yields false", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_04 | N | Correct Repository Method Called
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallRepositoryMethod()
        {
            var mockRepo = MockBlogRepository.GetMock();
            mockRepo.Setup(x => x.IncreaseViewCountAsync("BLOG-1")).ReturnsAsync(true);

            var command = new IncreaseViewCountCommand { BlogId = "BLOG-1" };
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.IncreaseViewCountAsync("BLOG-1"), Times.Once);

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_04",
                Description       = "Verify IncreaseViewCountAsync is invoked",
                ExpectedResult    = "IncreaseViewCountAsync called x1 with correct ID",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository method call verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_05 | A | Database Exception bubbles up or handled
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryException_ShouldThrow()
        {
            var mockRepo = MockBlogRepository.GetMock();
            mockRepo.Setup(x => x.IncreaseViewCountAsync("BLOG-1"))
                    .ThrowsAsync(new Exception("Database Error"));

            var command = new IncreaseViewCountCommand { BlogId = "BLOG-1" };
            
            Func<Task> act = async () => await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>().WithMessage("Database Error");

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_05",
                Description       = "Repository throws an exception",
                ExpectedResult    = "Exception bubbles up (if unhandled)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Simulate Exception", "Exception thrown" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Increase_View_Count_06 | N | Success Response Formatting
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldHaveCorrectResponse()
        {
            var mockRepo = MockBlogRepository.GetMock();
            mockRepo.Setup(x => x.IncreaseViewCountAsync("BLOG-1")).ReturnsAsync(true);

            var command = new IncreaseViewCountCommand { BlogId = "BLOG-1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Blog - Increase View Count", new TestCaseDetail
            {
                FunctionGroup     = "Increase View Count",
                TestCaseID        = "Increase_View_Count_06",
                Description       = "Check formatting of OperationResult on success",
                ExpectedResult    = "Success payload conforms to standard",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Validate Message and Data" }
            });
        }
    }
}
