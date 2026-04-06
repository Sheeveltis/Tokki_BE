using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Comments.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Comments
{
    /// <summary>
    /// Branch-coverage tests for CreateCommentCommandHandler:
    /// blog not found, parent comment not found, parent comment has own parent (flatten),
    /// successful creation, and repository exception.
    /// </summary>
    public class CreateCommentBranchCoverageTests
    {
        private readonly Mock<ICommentRepository>  _commentRepo = new();
        private readonly Mock<IIdGeneratorService> _idGen       = new();
        private readonly Mock<IBlogRepository>     _blogRepo    = new();

        private CreateCommentCommandHandler CreateHandler()
        {
            _idGen.Setup(x => x.Generate(15)).Returns("comment-id");
            return new CreateCommentCommandHandler(_commentRepo.Object, _idGen.Object, _blogRepo.Object);
        }

        // B01: Blog does not exist → 404
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(false);

            var result = await CreateHandler().Handle(
                new CreateCommentCommand { BlogId = "blog-1", Content = "hi", UserId = "u1" },
                CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B01",
                Description = "Blog does not exist → 404 BlogNotFound",
                ExpectedResult = "404", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExistsAsync returns false" }
            });
        }

        // B02: Parent comment not found → 404
        [Fact]
        public async Task Handle_ParentCommentNotFound_ShouldReturn404()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(true);
            _commentRepo.Setup(x => x.GetByIdAsync("parent-1")).ReturnsAsync((Comment?)null);

            var result = await CreateHandler().Handle(
                new CreateCommentCommand { BlogId = "blog-1", Content = "reply", UserId = "u1", ParentId = "parent-1" },
                CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B02",
                Description = "ParentId provided but parent comment not found → 404",
                ExpectedResult = "404 CommentNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync for parentId returns null" }
            });
        }

        // B03: Parent comment has own parentId → flatten (use grandparent's id)
        [Fact]
        public async Task Handle_ParentHasParent_ShouldFlattenToGrandparent()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(true);
            // parent-1 itself has a parent "root-comment"
            _commentRepo.Setup(x => x.GetByIdAsync("parent-1"))
                        .ReturnsAsync(new Comment { CommentId = "parent-1", ParentId = "root-comment" });
            _commentRepo.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            _commentRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new CreateCommentCommand { BlogId = "blog-1", Content = "nested reply", UserId = "u1", ParentId = "parent-1" };
            var result = await CreateHandler().Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // The saved comment's ParentId should be "root-comment" (flattened)
            _commentRepo.Verify(x => x.AddAsync(It.Is<Comment>(c => c.ParentId == "root-comment")), Times.Once);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B03",
                Description = "Nested reply: ParentId flattened to grandparent's id",
                ExpectedResult = "Comment.ParentId = 'root-comment', 201 success", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "parent.ParentId != null → request.ParentId = parent.ParentId" }
            });
        }

        // B04: First-level reply (parent has no parent) → parentId kept as-is
        [Fact]
        public async Task Handle_FirstLevelParent_ShouldKeepParentId()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(true);
            _commentRepo.Setup(x => x.GetByIdAsync("root-1"))
                        .ReturnsAsync(new Comment { CommentId = "root-1", ParentId = null });
            _commentRepo.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            _commentRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateCommentCommand { BlogId = "blog-1", Content = "reply", UserId = "u1", ParentId = "root-1" },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _commentRepo.Verify(x => x.AddAsync(It.Is<Comment>(c => c.ParentId == "root-1")), Times.Once);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B04",
                Description = "First-level reply: parent.ParentId is null → ParentId kept as original",
                ExpectedResult = "Comment.ParentId = 'root-1'", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "parent.ParentId == null → no flattening" }
            });
        }

        // B05: No ParentId → top-level comment 201
        [Fact]
        public async Task Handle_TopLevelComment_ShouldReturn201()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(true);
            _commentRepo.Setup(x => x.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);
            _commentRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateCommentCommand { BlogId = "blog-1", Content = "top level", UserId = "u1" },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data!.Content.Should().Be("top level");

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B05",
                Description = "Top-level comment (no ParentId) created successfully → 201",
                ExpectedResult = "201, Comment returned", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ParentId = null → no parent check" }
            });
        }

        // B06: Repository throws → 500
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            _blogRepo.Setup(x => x.ExistsAsync("blog-1")).ReturnsAsync(true);
            _commentRepo.Setup(x => x.AddAsync(It.IsAny<Comment>()))
                        .ThrowsAsync(new Exception("DB fail"));

            var result = await CreateHandler().Handle(
                new CreateCommentCommand { BlogId = "blog-1", Content = "hi", UserId = "u1" },
                CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler", TestCaseID = "TC-CC-B06",
                Description = "AddAsync throws → outer catch → 500 ServerError",
                ExpectedResult = "500", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws Exception" }
            });
        }
    }
}
