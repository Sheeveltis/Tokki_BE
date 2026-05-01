using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class GetBlogByIdQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetBlogByIdQueryHandler CreateHandler(
            Mock<IBlogRepository>? blogRepo = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetBlogByIdQueryHandler(
                (blogRepo ?? MockBlogRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object
            );
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_01 | A | Blog Not Found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlogNotFound_ShouldReturn404()
        {
            var query = new GetBlogByIdQuery { Id = "MISSING-BLOG" };
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.BlogNotFound);

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_01",
                Description       = "Provide an ID that does not exist in DB",
                ExpectedResult    = "Return 404 BlogNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_02 | N | Valid request, Author Exists → Return DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_WithAuthorInfo_ShouldReturn200()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.AuthorId = "USER-1";
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var author = MockAccountRepository.GetActiveUser("USER-1");
            author.FullName = "John Doe";
            var mockAccountRepo = MockAccountRepository.GetMock(new List<Account> { author });

            var query = new GetBlogByIdQuery { Id = "BLOG-1" };
            var result = await CreateHandler(mockBlogRepo, mockAccountRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Author.Should().NotBeNull();
            result.Data.Author.FullName.Should().Be("John Doe");

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_02",
                Description       = "Fetch an existing blog with a valid author",
                ExpectedResult    = "Return 200 with populated Author info",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Blog ID", "Valid AuthorId", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_03 | A | Valid request, Author Missing → Anonymous
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_MissingAuthor_ShouldReturnAnonymous()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.AuthorId = "GHOST-USER"; // Does not exist in mocked list
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var mockAccountRepo = MockAccountRepository.GetMock(new List<Account>()); // Empty accounts

            var query = new GetBlogByIdQuery { Id = "BLOG-1" };
            var result = await CreateHandler(mockBlogRepo, mockAccountRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Author.FullName.Should().Be("Người dùng ẩn danh");

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_03",
                Description       = "Fetch an existing blog where author was deleted/missing",
                ExpectedResult    = "Return 200, Author name defaults to 'Người dùng ẩn danh'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Blog ID", "Missing AuthorId", "Default Author full name" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_04 | N | Check Tags mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithTags_ShouldMapTagsCorrectly()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.Tags = new List<Tag> 
            {
                new Tag { Name = "C#" },
                new Tag { Name = ".NET" }
            };
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var query = new GetBlogByIdQuery { Id = "BLOG-1" };
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Tags.Should().ContainInOrder("C#", ".NET");

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_04",
                Description       = "Fetch blog containing multiple Tags",
                ExpectedResult    = "Return 200, Tags list is properly mapped to string list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Blog contains tags", "Select Tag.Name" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_05 | N | Category Mapping Fallback
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithoutCategoryInclude_ShouldReturnNA()
        {
            var blog = MockBlogRepository.GetSampleBlog("BLOG-1");
            blog.Category = null; // simulate disconnected graph
            
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });

            var query = new GetBlogByIdQuery { Id = "BLOG-1" };
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.CategoryName.Should().Be("N/A");

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_05",
                Description       = "Fetch blog when joined Category object is null",
                ExpectedResult    = "Return 200, CategoryName property defaults to 'N/A'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null Category property handling" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Blog_By_Id_06 | B | Empty Request ID → Returns 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyId_ShouldReturn404()
        {
            var query = new GetBlogByIdQuery { Id = "" };
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Blog - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "Get Blog By Id",
                TestCaseID        = "Get_Blog_By_Id_06",
                Description       = "Pass empty ID string to handler",
                ExpectedResult    = "Lookup fails, returns 404",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty ID yields null", "Return 404" }
            });
        }
    }
}
