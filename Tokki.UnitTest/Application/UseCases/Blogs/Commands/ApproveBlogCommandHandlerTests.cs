using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Blogs.Commands.ApproveBlog;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class ApproveBlogCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static ApproveBlogCommandHandler CreateHandler(
            Mock<IBlogRepository>? blogRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            string currentUserId = "ADMIN-001")
        {
            blogRepo ??= MockBlogRepository.GetMock();
            accountRepo ??= MockAccountRepository.GetMock();

            if (emailService == null)
            {
                emailService = new Mock<IEmailService>();
                emailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);
            }

            var emailHelper = new EmailNotificationHelper(emailService.Object);

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

            var logger = new Mock<ILogger<ApproveBlogCommandHandler>>();

            return new ApproveBlogCommandHandler(
                blogRepo.Object,
                accountRepo.Object,
                emailHelper,
                mockHttpContextAccessor.Object,
                logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-01 | A | Unauthorized Access (No UserId in Context) → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var handler = CreateHandler(currentUserId: "");
            var command = new ApproveBlogCommand { BlogId = "BLOG-123" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(AppErrors.UserUnauthorized);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-01",
                Description       = "Missing user id in HttpContext",
                ExpectedResult    = "Return 401 UserUnauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CurrentUser is empty", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-02 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var handler = CreateHandler();
            var command = new ApproveBlogCommand { BlogId = "MISSING-BLOG" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-02",
                Description       = "Blog ID does not exist in db",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync return null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-03 | A | Blog status is not Pending Approval → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotPending_ShouldReturn400()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.Draft);
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(blogRepo: mockRepo);
            var command = new ApproveBlogCommand { BlogId = "BLOG-123" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(AppErrors.BlogInvalidPending);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-03",
                Description       = "Blog is in Draft instead of PendingApproval state",
                ExpectedResult    = "Return 400 BlogInvalidPending",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != PendingApproval", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-04 | N | Valid request → Approves blog and sends email
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldApproveAndEmail()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            blog.AuthorId = "USER-1";
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var user = MockAccountRepository.GetActiveUser("USER-1", "author@test.com");
            var mockAccRepo = MockAccountRepository.GetMock(new List<Tokki.Domain.Entities.Account> { user });
            mockAccRepo.Setup(x => x.GetByIdAsync("USER-1")).ReturnsAsync(user);

            var mockEmail = new Mock<IEmailService>();

            var handler = CreateHandler(mockBlogRepo, mockAccRepo, mockEmail);
            var command = new ApproveBlogCommand { BlogId = "BLOG-123" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify status changed
            blog.Status.Should().Be(BlogStatus.Published);
            mockBlogRepo.Verify(x => x.UpdateAsync(blog), Times.Once);

            // Verify email sent
            mockEmail.Verify(x => x.SendEmailAsync("author@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-04",
                Description       = "Approve pending blog with valid author email",
                ExpectedResult    = "Return 200, status updated to Published, send email",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid pending blog", "Author has email", "SendContentApprovedAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-05 | N | Author not found or no email → Still Approve, No Email
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AuthorNotFound_ShouldStillApproveWithoutEmail()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            // Non-existent author
            blog.AuthorId = "GHOST"; 
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var mockEmail = new Mock<IEmailService>();

            var handler = CreateHandler(mockBlogRepo, emailService: mockEmail);
            var command = new ApproveBlogCommand { BlogId = "BLOG-123" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            blog.Status.Should().Be(BlogStatus.Published);
            
            // Should not send email
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-05",
                Description       = "Approve pending blog where author is not found",
                ExpectedResult    = "Return 200, status Published, NO email sent",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AuthorId missing/invalid", "Skip email step", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AB-06 | N | UpdateAsync correctly sets UpdatedAt
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldSetUpdatedAtTime()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            blog.UpdatedAt = null;
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockBlogRepo);
            var command = new ApproveBlogCommand { BlogId = "BLOG-123" };
            
            await handler.Handle(command, CancellationToken.None);

            blog.UpdatedAt.Should().NotBeNull();
            mockBlogRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Blog>(b => b.UpdatedAt != null)), Times.Once);

            QACollector.LogTestCase("Blog - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Blog",
                TestCaseID        = "TC-AB-06",
                Description       = "Check if UpdatedAt timestamp is populated",
                ExpectedResult    = "UpdatedAt is set before saving to DB",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Check UpdatedAt property" }
            });
        }
    }
}
