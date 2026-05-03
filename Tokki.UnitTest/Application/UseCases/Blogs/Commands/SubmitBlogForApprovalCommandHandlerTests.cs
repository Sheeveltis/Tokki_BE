using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class SubmitBlogForApprovalCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static SubmitBlogForApprovalCommandHandler CreateHandler(
            Mock<IBlogRepository>? repo = null,
            string currentUserId = "USER-1")
        {
            repo ??= MockBlogRepository.GetMock();
            
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                var mockHttpContext = new DefaultHttpContext { User = claimsPrincipal };
                mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);
            }
            else
            {
                mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }

            var logger = new Mock<ILogger<SubmitBlogForApprovalCommandHandler>>();
            var bgJobClient = new Mock<Hangfire.IBackgroundJobClient>();

            return new SubmitBlogForApprovalCommandHandler(repo.Object, mockHttpContextAccessor.Object, bgJobClient.Object, logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_01 | A | Unauthorized (No UserId context) → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var handler = CreateHandler(currentUserId: "");
            var command = new SubmitBlogForApprovalCommand { BlogId = "BLOG-1" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(AppErrors.UserUnauthorized);

            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_01",
                Description       = "Missing user id in HttpContext payload",
                ExpectedResult    = "Return 401 UserUnauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty user identity", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_02 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new SubmitBlogForApprovalCommand { BlogId = "MISSING-BLOG" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_02",
                Description       = "Attempt to submit a non-existent blog",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_03 | A | User is not the Author → 403 Forbidden
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotAuthor_ShouldReturn403()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1", BlogStatus.Draft);
            blog.AuthorId = "USER-1";
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockRepo, currentUserId: "USER-999"); // Different user
            var command = new SubmitBlogForApprovalCommand { BlogId = "BLOG-1" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().Contain(AppErrors.IsNotAuthor);

            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_03",
                Description       = "User attempting submission is not the blog author",
                ExpectedResult    = "Return 403 IsNotAuthor",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AuthorId mismatch", "Return 403" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_04 | A | Blog Status is Invalid (e.g. Published) → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InvalidBlogStatus_ShouldReturn400()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1", BlogStatus.Published); // Invalid state
            blog.AuthorId = "USER-1";
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockRepo, currentUserId: "USER-1");
            var command = new SubmitBlogForApprovalCommand { BlogId = "BLOG-1" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(AppErrors.BlogInvalidPending);

            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_04",
                Description       = "Attempt to submit a blog that is already Published",
                ExpectedResult    = "Return 400 BlogInvalidPending",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Published", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_05 | N | Valid Draft Status → PendingApproval and 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidDraftBlog_ShouldReturn200()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1", BlogStatus.Draft);
            blog.AuthorId = "USER-1";
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockRepo, currentUserId: "USER-1");
            var command = new SubmitBlogForApprovalCommand { BlogId = "BLOG-1" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            blog.Status.Should().Be(BlogStatus.PendingApproval);
            mockRepo.Verify(x => x.UpdateAsync(blog), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_05",
                Description       = "Author submits a Draft blog",
                ExpectedResult    = "Return 200, status updated to PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Draft limits", "Status = PendingApproval", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Blog_For_Approval_06 | N | Valid Rejected Status → PendingApproval and 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRejectedBlog_ShouldSubmitAgain()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1", BlogStatus.Rejected);
            blog.AuthorId = "USER-1";
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockRepo, currentUserId: "USER-1");
            var command = new SubmitBlogForApprovalCommand { BlogId = "BLOG-1" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            blog.Status.Should().Be(BlogStatus.PendingApproval);
            
            QACollector.LogTestCase("Blog - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Blog For Approval",
                TestCaseID        = "Submit_Blog_For_Approval_06",
                Description       = "Author re-submits a previously Rejected blog",
                ExpectedResult    = "Return 200, status updated back to PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Rejected states allowed", "Status = PendingApproval", "Return 200" }
            });
        }
    }
}
