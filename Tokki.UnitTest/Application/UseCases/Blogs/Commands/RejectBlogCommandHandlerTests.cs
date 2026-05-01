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
using Tokki.Application.UseCases.Blogs.Commands.RejectBlog;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class RejectBlogCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static RejectBlogCommandHandler CreateHandler(
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

            var logger = new Mock<ILogger<RejectBlogCommandHandler>>();

            return new RejectBlogCommandHandler(
                blogRepo.Object,
                accountRepo.Object,
                emailHelper,
                mockHttpContextAccessor.Object,
                logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_01 | A | Unauthorized Access (No UserId) → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var handler = CreateHandler(currentUserId: "");
            var command = new RejectBlogCommand { BlogId = "BLOG-123", RejectReason = "Violates ToS" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(AppErrors.UserUnauthorized);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_01",
                Description       = "Missing user id in HttpContext",
                ExpectedResult    = "Return 401 UserUnauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CurrentUser is empty", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_02 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var handler = CreateHandler();
            var command = new RejectBlogCommand { BlogId = "MISSING-BLOG", RejectReason = "Reason" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_02",
                Description       = "Blog ID does not exist in DB",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync return null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_03 | A | Blog status is not Pending Approval → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotPending_ShouldReturn400()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.Published);
            var mockRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(blogRepo: mockRepo);
            var command = new RejectBlogCommand { BlogId = "BLOG-123", RejectReason = "Reason" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(AppErrors.BlogInvalidPending);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_03",
                Description       = "Attempt to reject a Published blog",
                ExpectedResult    = "Return 400 BlogInvalidPending",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != PendingApproval", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_04 | N | Valid request → Rejects blog and sends email
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldRejectAndEmail()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            blog.AuthorId = "USER-1";
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var user = MockAccountRepository.GetActiveUser("USER-1", "author@test.com");
            var mockAccRepo = MockAccountRepository.GetMock(new List<Tokki.Domain.Entities.Account> { user });
            mockAccRepo.Setup(x => x.GetByIdAsync("USER-1")).ReturnsAsync(user);

            var mockEmail = new Mock<IEmailService>();

            var handler = CreateHandler(mockBlogRepo, mockAccRepo, mockEmail);
            var command = new RejectBlogCommand { BlogId = "BLOG-123", RejectReason = "Lý do từ chối" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify status changed
            blog.Status.Should().Be(BlogStatus.Rejected);
            mockBlogRepo.Verify(x => x.UpdateAsync(blog), Times.Once);

            // Verify email sent
            mockEmail.Verify(x => x.SendEmailAsync("author@test.com", It.IsAny<string>(), It.Is<string>(html => html.Contains("Lý do từ chối"))), Times.Once);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_04",
                Description       = "Reject pending blog with valid author email",
                ExpectedResult    = "Return 200, status updated to Rejected, send email with reason",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid pending blog", "SendContentRejectedAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_05 | N | Author not found → Still Reject, No Email
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AuthorNotFound_ShouldStillRejectWithoutEmail()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            blog.AuthorId = "GHOST"; 
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var mockEmail = new Mock<IEmailService>();

            var handler = CreateHandler(mockBlogRepo, emailService: mockEmail);
            var command = new RejectBlogCommand { BlogId = "BLOG-123", RejectReason = "Reason" };
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            blog.Status.Should().Be(BlogStatus.Rejected);
            
            mockEmail.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_05",
                Description       = "Reject pending blog where author is not found",
                ExpectedResult    = "Return 200, status Rejected, NO email sent",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AuthorId missing", "Skip email step", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Reject_Blog_06 | N | Verify UpdatedAt is set
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldSetUpdatedAt()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-123", BlogStatus.PendingApproval);
            blog.UpdatedAt = null;
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Tokki.Domain.Entities.Blog> { blog });
            
            var handler = CreateHandler(mockBlogRepo);
            var command = new RejectBlogCommand { BlogId = "BLOG-123", RejectReason = "Reason" };
            
            await handler.Handle(command, CancellationToken.None);

            blog.UpdatedAt.Should().NotBeNull();
            mockBlogRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Blog>(b => b.UpdatedAt != null)), Times.Once);

            QACollector.LogTestCase("Blog - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Blog",
                TestCaseID        = "Reject_Blog_06",
                Description       = "Ensure the UpdatedAt timestamp is populated",
                ExpectedResult    = "UpdatedAt is updated prior to saving",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Check UpdatedAt property" }
            });
        }
    }
}
