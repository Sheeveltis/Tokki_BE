using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Comments.Commands
{
    public class CreateCommentCommandHandlerTests
    {
        private readonly Mock<ICommentRepository> _commentRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();
        private readonly Mock<IBlogRepository> _blogRepoMock = new();

        private CreateCommentCommandHandler CreateHandler()
        {
            return new CreateCommentCommandHandler(_commentRepoMock.Object, _idGenMock.Object, _blogRepoMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_01 | A | Blog Not Found -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new CreateCommentCommand { BlogId = "blog-123", Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(false);
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.BlogNotFound.Code);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_01",
                Description = "Returns error if blog does not exist",
                ExpectedResult = "Return 404 BlogNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Blog ExistsAsync returns false" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_02 | A | Parent Comment Provided But Not Found -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ParentCommentNotFound_ShouldReturn404()
        {
            var command = new CreateCommentCommand { BlogId = "blog-123", ParentId = "parent-123", Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(true);
            _commentRepoMock.Setup(x => x.GetByIdAsync(command.ParentId)).ReturnsAsync((Comment?)null);
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.CommentNotFound.Code);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_02",
                Description = "Returns error if parent comment ID provided but not found",
                ExpectedResult = "Return 404 CommentNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Parent string not empty", "Parent GetById returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_03 | N | Parent Comment Provided Has Null ParentId
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ParentHasNoParent_ShouldAssignDirectParent()
        {
            var command = new CreateCommentCommand { BlogId = "blog-1", ParentId = "parent-1", Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(true);
            var directParent = new Comment { CommentId = "parent-1", ParentId = null };
            _commentRepoMock.Setup(x => x.GetByIdAsync(command.ParentId)).ReturnsAsync(directParent);
            _idGenMock.Setup(x => x.Generate(15)).Returns("cmt-new-id");
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            _commentRepoMock.Verify(x => x.AddAsync(It.Is<Comment>(c => c.ParentId == "parent-1")), Times.Once);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_03",
                Description = "Nested comment correctly assigns parent ID when parent has no parent",
                ExpectedResult = "Return 201 Success",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Parent exists and has no parent" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_04 | N | Parent Comment Already Nested (Has Parent) -> Overrides ParentId To Root
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NestedParent_ShouldFlattenToRootParent()
        {
            var command = new CreateCommentCommand { BlogId = "blog-1", ParentId = "level2-cmt", Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(true);
            var nestedParent = new Comment { CommentId = "level2-cmt", ParentId = "root-parent" };
            _commentRepoMock.Setup(x => x.GetByIdAsync(command.ParentId)).ReturnsAsync(nestedParent);
            _idGenMock.Setup(x => x.Generate(15)).Returns("cmt-new-id");
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            _commentRepoMock.Verify(x => x.AddAsync(It.Is<Comment>(c => c.ParentId == "root-parent")), Times.Once);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_04",
                Description = "If replying to a reply, flattens mapping to root parent",
                ExpectedResult = "Return 201 Success with changed ParentId",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Parent has ParentId mapped already" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_05 | N | No Parent ID Given -> Root Comment Creation
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoParentGiven_ShouldCreateRootComment()
        {
            var command = new CreateCommentCommand { BlogId = "blog-1", ParentId = null, Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(true);
            _idGenMock.Setup(x => x.Generate(15)).Returns("cmt-new-id");
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            _commentRepoMock.Verify(x => x.AddAsync(It.Is<Comment>(c => c.ParentId == null)), Times.Once);
            _commentRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_05",
                Description = "Creates a standard root comment when ParentId is null",
                ExpectedResult = "Return 201 Success root comment",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ParentId is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateCommentCommandHandler_06 | E | Database Add Fails Throws Exception -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500()
        {
            var command = new CreateCommentCommand { BlogId = "blog-1", Content = "Test", UserId = "user-1" };
            _blogRepoMock.Setup(x => x.ExistsAsync(command.BlogId)).ReturnsAsync(true);
            _commentRepoMock.Setup(x => x.AddAsync(It.IsAny<Comment>())).ThrowsAsync(new Exception("DB Down"));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateCommentCommandHandler",
                TestCaseID = "CreateCommentCommandHandler_06",
                Description = "Catches general exception and returns 500 server error",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception thrown during AddAsync" }
            });
        }
    }
}
