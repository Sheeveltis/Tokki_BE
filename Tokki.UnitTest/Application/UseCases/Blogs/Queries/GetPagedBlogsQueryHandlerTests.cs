using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs
{
    public class GetPagedBlogsQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetPagedBlogsQueryHandler CreateHandler(
            Mock<IBlogRepository>? blogRepo = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetPagedBlogsQueryHandler(
                (blogRepo ?? MockBlogRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object
            );
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-01 | N | Empty Results → Return 200 with Count 0
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoBlogs_ShouldReturnEmptyPagedResult()
        {
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog>());
            var query = new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 };
            
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-01",
                Description       = "Request paged blogs when database is empty",
                ExpectedResult    = "Return 200 Success with TotalCount = 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB has no blogs", "Verify Empty list mapping" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-02 | N | Pagination Limits logic
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Pagination_ShouldRespectLimits()
        {
            var blogs = Enumerable.Range(1, 15).Select(i => MockBlogRepository.GetSampleBlog($"BLOG-{i}", BlogStatus.Published)).ToList();
            var mockBlogRepo = MockBlogRepository.GetMock(blogs);
            
            var query = new GetPagedBlogsQuery { PageNumber = 2, PageSize = 10 };
            
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(5); // Page 2 of 15 items with size 10
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-02",
                Description       = "Request second page with explicit size",
                ExpectedResult    = "Return exactly the remaining items on second page",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber = 2", "PageSize = 10" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-03 | N | Filters -> CategoryId & Status
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithFilters_ShouldReturnFilteredSubset()
        {
            var blogs = new List<Blog>
            {
                MockBlogRepository.GetSampleBlog("B1", BlogStatus.Published),
                MockBlogRepository.GetSampleBlog("B2", BlogStatus.Draft)
            };
            blogs[0].CategoryId = "TECH";
            blogs[1].CategoryId = "TECH";
            
            var mockBlogRepo = MockBlogRepository.GetMock(blogs);
            
            var query = new GetPagedBlogsQuery { CategoryId = "TECH", Status = BlogStatus.Published };
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().Id.Should().Be("B1");

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-03",
                Description       = "Apply both CategoryId and Status filters via query",
                ExpectedResult    = "Repository receives filters, returns limited subset",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Test filtering logic" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-04 | N | Author Mapping Resolution
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AuthorMapping_ShouldMapAccountInfo()
        {
            var blog = MockBlogRepository.GetSampleBlog("B1");
            blog.AuthorId = "USER-1";
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });
            
            var account = MockAccountRepository.GetActiveUser("USER-1");
            account.FullName = "Mapped User";
            var mockAccountRepo = MockAccountRepository.GetMock(new List<Account> { account });

            var query = new GetPagedBlogsQuery();
            var result = await CreateHandler(mockBlogRepo, mockAccountRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().Author.Should().NotBeNull();
            result.Data.Items.First().Author.FullName.Should().Be("Mapped User");

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-04",
                Description       = "Map AuthorId from Blog entity to Account details",
                ExpectedResult    = "Author full name properly mapped in DTO array",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Accounts properly mocked" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-05 | A | Missing Author defaults to "Người dùng ẩn danh"
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AuthorNotFound_ShouldDefaultAuthorName()
        {
            var blog = MockBlogRepository.GetSampleBlog("B1");
            blog.AuthorId = "GHOST";
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });
            
            var mockAccountRepo = MockAccountRepository.GetMock(new List<Account>());

            var query = new GetPagedBlogsQuery();
            var result = await CreateHandler(mockBlogRepo, mockAccountRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().Author.FullName.Should().Be("Người dùng ẩn danh");

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-05",
                Description       = "Author account missing or deleted",
                ExpectedResult    = "DTO falls back to 'Người dùng ẩn danh'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Simulate orphaned relation" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GPB-06 | A | Missing Category null check
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CategoryNull_ShouldReturnUnknownCategory()
        {
            var blog = MockBlogRepository.GetSampleBlog("B1");
            blog.Category = null; // No eager loading simulated
            var mockBlogRepo = MockBlogRepository.GetMock(new List<Blog> { blog });
            
            var query = new GetPagedBlogsQuery();
            var result = await CreateHandler(mockBlogRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().CategoryName.Should().Be("Không xác định");

            QACollector.LogTestCase("Blog - Get Paged Blogs", new TestCaseDetail
            {
                FunctionGroup     = "Get Paged Blogs",
                TestCaseID        = "TC-GPB-06",
                Description       = "Blog category navigation property is null",
                ExpectedResult    = "CategoryName property defaults to 'Không xác định'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Simulate detached Category property" }
            });
        }
    }
}
