using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Comments.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Comments
{
    public class CreateCommentCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static CreateCommentCommandHandler CreateHandler(
            Mock<ICommentRepository>? commentRepo = null,
            Mock<IBlogRepository>?   blogRepo     = null)
        {
            var mockComment = commentRepo ?? new Mock<ICommentRepository>();
            var mockBlog    = blogRepo    ?? new Mock<IBlogRepository>();

            mockBlog.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return new CreateCommentCommandHandler(
                mockComment.Object,
                MockIdGeneratorService.GetMock().Object,
                mockBlog.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_01 | A | Blog not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            // Arrange
            var mockBlog = new Mock<IBlogRepository>();
            mockBlog.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var command = new CreateCommentCommand { BlogId = "BLOG-INVALID", Content = "Test", UserId = "U-001" };

            // Act
            var result = await CreateHandler(blogRepo: mockBlog).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_01",
                Description       = "Create comment with BlogId that does not exist",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExistsAsync returns false", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_02 | A | ParentComment not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ParentCommentNotFound_ShouldReturn404()
        {
            // Arrange
            var mockComment = new Mock<ICommentRepository>();
            mockComment.Setup(x => x.GetByIdAsync("CMT-GHOST")).ReturnsAsync((Comment?)null);
            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateCommentCommand
            {
                BlogId   = "BLOG-001",
                Content  = "Reply",
                UserId   = "U-001",
                ParentId = "CMT-GHOST"
            };

            // Act
            var result = await CreateHandler(commentRepo: mockComment).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_02",
                Description       = "Reply to a ParentId that does not exist in the repository",
                ExpectedResult    = "Return 404 CommentNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ParentId specified", "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_03 | N | Valid comment → 201 Created
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidComment_ShouldReturn201()
        {
            // Arrange
            var command = new CreateCommentCommand { BlogId = "BLOG-001", Content = "Great post!", UserId = "U-001" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Content.Should().Be("Great post!");

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_03",
                Description       = "Create a top-level comment on a valid blog",
                ExpectedResult    = "Return 201 Created with populated CommentDTO",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid BlogId", "No ParentId", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_04 | N | Reply to nested comment flattens to top-level
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReplyToNestedComment_ShouldFlattenParentId()
        {
            // Arrange
            var parentComment = new Comment { CommentId = "CMT-002", ParentId = "CMT-001" };

            var mockComment = new Mock<ICommentRepository>();
            mockComment.Setup(x => x.GetByIdAsync("CMT-002")).ReturnsAsync(parentComment);
            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateCommentCommand
            {
                BlogId   = "BLOG-001",
                Content  = "Reply to reply",
                UserId   = "U-001",
                ParentId = "CMT-002"
            };

            // Act
            var result = await CreateHandler(commentRepo: mockComment).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            command.ParentId.Should().Be("CMT-001"); // flattened

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_04",
                Description       = "Reply to a nested comment — ParentId should be flattened to the top-level parent",
                ExpectedResult    = "Command.ParentId = CMT-001 (top-level), return 201",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Parent has non-null ParentId", "Flatten to parent.ParentId" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_05 | A | Repository throws exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var mockComment = new Mock<ICommentRepository>();
            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>())).ThrowsAsync(new Exception("DB Error"));
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateCommentCommand { BlogId = "BLOG-001", Content = "Comment", UserId = "U-001" };

            // Act
            var result = await CreateHandler(commentRepo: mockComment).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_05",
                Description       = "AddAsync throws exception during persistence",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws Exception", "try/catch returns 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Comment_06 | N | DTO UserId matches command UserId
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidComment_DtoUserIdMatchesCommand()
        {
            // Arrange
            var command = new CreateCommentCommand { BlogId = "BLOG-001", Content = "Hello", UserId = "U-XYZ" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.UserId.Should().Be("U-XYZ");

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Comment",
                TestCaseID        = "Create_Comment_06",
                Description       = "Verify the returned DTO carries the same UserId as the command",
                ExpectedResult    = "Data.UserId = 'U-XYZ'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DTO projection checks", "UserId field forwarded correctly" }
            });
        }
    }
}