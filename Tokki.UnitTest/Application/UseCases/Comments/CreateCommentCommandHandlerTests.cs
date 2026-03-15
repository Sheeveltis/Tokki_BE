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
        private CreateCommentCommandHandler CreateHandler(
            Mock<ICommentRepository>? commentRepo = null,
            Mock<IBlogRepository>? blogRepo = null)
        {
            var mockComment = commentRepo ?? new Mock<ICommentRepository>();
            var mockBlog = blogRepo ?? new Mock<IBlogRepository>();

            // Default: blog exists
            mockBlog.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);

            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>()))
                       .Returns(Task.CompletedTask);
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            return new CreateCommentCommandHandler(
                mockComment.Object,
                MockIdGeneratorService.GetMock().Object,
                mockBlog.Object);
        }

        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var command = new CreateCommentCommand
            {
                BlogId = "BLOG-INVALID",
                Content = "Bình luận test",
                UserId = "USER-001"
            };

            var mockBlog = new Mock<IBlogRepository>();
            mockBlog.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false); // blog không tồn tại

            var handler = CreateHandler(blogRepo: mockBlog);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Comment",
                TestCaseID = "TC-CMT-CRE-01",
                Description = "Tạo comment với BlogId không tồn tại",
                ExpectedResult = "Return 404 BlogNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid BlogId",
                    "Blog không tồn tại",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidComment_ShouldReturn201()
        {
            var command = new CreateCommentCommand
            {
                BlogId = "BLOG-001",
                Content = "Bình luận hay lắm!",
                UserId = "USER-001"
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Content.Should().Be("Bình luận hay lắm!");

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Comment",
                TestCaseID = "TC-CMT-CRE-02",
                Description = "Tạo comment hợp lệ → return 201",
                ExpectedResult = "Return 201, Data.Content = content",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid BlogId",
                    "Valid Content",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_ReplyToComment_ParentIsNested_ShouldFlattenToTopLevel()
        {
            // ParentId của parent có giá trị → flatten lên top-level
            var command = new CreateCommentCommand
            {
                BlogId = "BLOG-001",
                Content = "Reply nested",
                UserId = "USER-001",
                ParentId = "CMT-002"
            };

            var parentComment = new Comment
            {
                CommentId = "CMT-002",
                ParentId = "CMT-001" // nested → flatten lên CMT-001
            };

            var mockComment = new Mock<ICommentRepository>();
            mockComment.Setup(x => x.GetByIdAsync("CMT-002"))
                       .ReturnsAsync(parentComment);
            mockComment.Setup(x => x.AddAsync(It.IsAny<Comment>()))
                       .Returns(Task.CompletedTask);
            mockComment.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var handler = CreateHandler(commentRepo: mockComment);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // ParentId nên được flatten về CMT-001
            command.ParentId.Should().Be("CMT-001");

            QACollector.LogTestCase("Comment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Comment",
                TestCaseID = "TC-CMT-CRE-03",
                Description = "Reply vào comment đã là nested → ParentId tự động flatten về top-level",
                ExpectedResult = "ParentId = CMT-001 (top-level), return 201",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "ParentComment.ParentId != null (nested)",
                    "Flatten ParentId lên top-level",
                    "Return 201"
                }
            });
        }
    }
}