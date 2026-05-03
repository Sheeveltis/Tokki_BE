using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Comments.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Comments
{
    public class GetCommentsQueryHandlerTests
    {
        // -----------------------------------------------------------
        // FACTORY
        // -----------------------------------------------------------
        private static GetCommentsQueryHandler CreateHandler(Mock<ICommentRepository>? repo = null)
        {
            var mockRepo = repo ?? new Mock<ICommentRepository>();
            return new GetCommentsQueryHandler(mockRepo.Object);
        }

        private static Mock<ICommentRepository> SetupRepo(List<Comment> comments)
        {
            var mock = new Mock<ICommentRepository>();
            mock.Setup(x => x.GetByBlogIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(comments);
            return mock;
        }

        private static Comment MakeComment(
            string id,
            string content,
            string userId   = "U-001",
            string? parent  = null,
            bool isDeleted  = false) => new()
        {
            CommentId  = id,
            Content    = content,
            UserId     = userId,
            ParentId   = parent,
            IsDeleted  = isDeleted,
            CreatedAt  = DateTimeOffset.UtcNow
        };

        // -----------------------------------------------------------
        // Get_Comments_01 | N | Empty blog ? empty list, 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NoBlogComments_ShouldReturnEmptyList()
        {
            // Arrange
            var mockRepo = SetupRepo(new List<Comment>());
            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_01",
                Description       = "Request comments for a blog with no comments yet",
                ExpectedResult    = "Return 200 Success with empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByBlogIdAsync returns empty", "Data is empty list" }
            });
        }

        // -----------------------------------------------------------
        // Get_Comments_02 | N | Root comments are returned at top level
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RootComments_ShouldReturnAtTopLevel()
        {
            // Arrange
            var comments = new List<Comment>
            {
                MakeComment("C1", "First"),
                MakeComment("C2", "Second")
            };
            var mockRepo = SetupRepo(comments);
            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.All(c => c.ParentId == null).Should().BeTrue();

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_02",
                Description       = "Fetch all root-level comments for a blog",
                ExpectedResult    = "Return 200 with 2 top-level comments",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All comments have null ParentId", "Top-level list count = 2" }
            });
        }

        // -----------------------------------------------------------
        // Get_Comments_03 | N | Replies are nested under parent
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ReplyComment_ShouldBeNestedUnderParent()
        {
            // Arrange
            var comments = new List<Comment>
            {
                MakeComment("C1", "Root"),
                MakeComment("C2", "Reply", parent: "C1")
            };
            var mockRepo = SetupRepo(comments);
            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.Data.Should().HaveCount(1); // only root at top level
            result.Data.First().Replies.Should().HaveCount(1);
            result.Data.First().Replies.First().Content.Should().Be("Reply");

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_03",
                Description       = "One root comment and one reply — reply should be nested in Replies list",
                ExpectedResult    = "Root count = 1, Root.Replies count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Reply has valid ParentId", "Nested under correct parent DTO" }
            });
        }

        // -----------------------------------------------------------
        // Get_Comments_04 | N | Deleted comments show masked content
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DeletedComment_ShouldShowMaskedContent()
        {
            // Arrange
            var comments = new List<Comment>
            {
                MakeComment("C1", "This was deleted", isDeleted: true)
            };
            var mockRepo = SetupRepo(comments);
            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.Data.First().Content.Should().Contain("dã b? xóa");
            result.Data.First().IsDeleted.Should().BeTrue();

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_04",
                Description       = "A soft-deleted comment should show a masked placeholder content",
                ExpectedResult    = "Content = localized 'deleted' message, IsDeleted = true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Comment.IsDeleted = true", "Content replaced by placeholder" }
            });
        }

        // -----------------------------------------------------------
        // Get_Comments_05 | A | Reply with orphan ParentId goes to top level
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_OrphanReply_ShouldFallBackToTopLevel()
        {
            // Arrange — parent"C-MISSING" doesn't exist in the list
            var comments = new List<Comment>
            {
                MakeComment("C1", "Orphan reply", parent: "C-MISSING")
            };
            var mockRepo = SetupRepo(comments);
            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert — orphan falls to root level
            result.Data.Should().HaveCount(1);
            result.Data.First().Content.Should().Be("Orphan reply");

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_05",
                Description       = "A reply whose parent no longer exists in the result set",
                ExpectedResult    = "Orphan reply is promoted to top-level list",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ParentId points to non-existent comment", "Falls through to rootComments" }
            });
        }

        // -----------------------------------------------------------
        // Get_Comments_06 | A | Repository throws exception ? bubbles up
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var mockRepo = new Mock<ICommentRepository>();
            mockRepo.Setup(x => x.GetByBlogIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB timeout"));

            var query = new GetCommentsQuery { BlogId = "BLOG-001" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => CreateHandler(mockRepo).Handle(query, CancellationToken.None));

            QACollector.LogTestCase("Comment - Get", new TestCaseDetail
            {
                FunctionGroup     = "Get Comments",
                TestCaseID        = "Get_Comments_06",
                Description       = "Repository throws an exception while fetching comments",
                ExpectedResult    = "Exception bubbles up to global exception handler",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByBlogIdAsync throws Exception", "No try/catch in handler" }
            });
        }
    }
}
