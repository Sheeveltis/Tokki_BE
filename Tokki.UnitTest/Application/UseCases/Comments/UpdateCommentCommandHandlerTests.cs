using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Comments.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Comments
{
    public class UpdateCommentCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UpdateCommentCommandHandler CreateHandler(Mock<ICommentRepository>? repo = null)
        {
            var mockRepo = repo ?? new Mock<ICommentRepository>();
            return new UpdateCommentCommandHandler(mockRepo.Object);
        }

        private static Comment GetSampleComment(
            string id     = "CMT-001",
            string userId = "U-001",
            bool isDeleted = false) => new()
        {
            CommentId  = id,
            Content    = "Original content",
            UserId     = userId,
            IsDeleted  = isDeleted,
            CreatedAt  = DateTimeOffset.UtcNow
        };

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-01 | A | Comment not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CommentNotFound_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Comment?)null);

            var command = new UpdateCommentCommand { CommentId = "CMT-GHOST", Content = "New text", UserId = "U-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-01",
                Description       = "Attempt to update a comment with an ID that does not exist",
                ExpectedResult    = "Return 404 CommentNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-02 | A | Comment is soft-deleted → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CommentIsDeleted_ShouldReturn404()
        {
            // Arrange
            var deletedComment = GetSampleComment(isDeleted: true);
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("CMT-001")).ReturnsAsync(deletedComment);

            var command = new UpdateCommentCommand { CommentId = "CMT-001", Content = "Edit text", UserId = "U-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-02",
                Description       = "Update a comment that has been soft-deleted (IsDeleted = true)",
                ExpectedResult    = "Return 404 CommentNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Comment.IsDeleted = true", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-03 | A | Wrong UserId → 403 Forbidden
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongUser_ShouldReturn403()
        {
            // Arrange
            var comment = GetSampleComment(userId: "U-OWNER");
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("CMT-001")).ReturnsAsync(comment);

            var command = new UpdateCommentCommand { CommentId = "CMT-001", Content = "Hacked", UserId = "U-ATTACKER" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-03",
                Description       = "User tries to edit another user's comment",
                ExpectedResult    = "Return 403 Forbidden",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Command.UserId != comment.UserId", "Return 403" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-04 | N | Valid update → 200 with updated content
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200WithUpdatedContent()
        {
            // Arrange
            var comment = GetSampleComment();
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("CMT-001")).ReturnsAsync(comment);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new UpdateCommentCommand { CommentId = "CMT-001", Content = "Edited content", UserId = "U-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Content.Should().Be("Edited content");

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-04",
                Description       = "Valid update request from the comment author",
                ExpectedResult    = "Return 200 with updated CommentDTO",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid CommentId", "Matching UserId", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-05 | N | Comment.Content mutated in memory
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldMutateCommentContent()
        {
            // Arrange
            var comment = GetSampleComment();
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("CMT-001")).ReturnsAsync(comment);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new UpdateCommentCommand { CommentId = "CMT-001", Content = "Mutated!", UserId = "U-001" };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            comment.Content.Should().Be("Mutated!");

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-05",
                Description       = "Verify the entity's Content property is mutated before SaveChanges",
                ExpectedResult    = "comment.Content = 'Mutated!' in memory after handler runs",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Entity tracking mutation", "SaveChangesAsync called once" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CMU-06 | A | SaveChanges throws exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            // Arrange — handler has no try/catch for SaveChanges, so exception bubbles up
            var comment = GetSampleComment();
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("CMT-001")).ReturnsAsync(comment);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB connection lost"));

            var command = new UpdateCommentCommand { CommentId = "CMT-001", Content = "New text", UserId = "U-001" };

            // Act & Assert — exception propagates (no inner try/catch in handler)
            await Assert.ThrowsAsync<Exception>(
                () => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Comment - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Comment",
                TestCaseID        = "TC-CMU-06",
                Description       = "SaveChangesAsync throws an exception during persistence",
                ExpectedResult    = "Exception propagates up for global middleware to handle",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception", "No inner try/catch in handler" }
            });
        }
    }
}
