using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Blogs.Queries
{
    public class GetPagedBlogsQueryHandlerTests
    {
        private readonly Mock<IBlogRepository> _blogRepoMock = new();
        private readonly Mock<IAccountRepository> _accountRepoMock = new();

        private GetPagedBlogsQueryHandler CreateHandler()
        {
            return new GetPagedBlogsQueryHandler(_blogRepoMock.Object, _accountRepoMock.Object);
        }

        // TC-BLG-GPB-01 | N | Empty Result -> 200
        [Fact]
        public async Task Handle_EmptyResult_ShouldReturnEmptyDtoList()
        {
            var pagedData = new PagedResult<Blog>(new List<Blog>(), 0, 1, 10);
            _blogRepoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
            
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>()))
                            .ReturnsAsync(new Dictionary<string, AccountBasicInfoDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-01",
                Description = "Empty collection handled gracefully",
                ExpectedResult = "Empty Items list",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB returns 0 records" }
            });
        }

        // TC-BLG-GPB-02 | N | Missing Author Info mapped cleanly
        [Fact]
        public async Task Handle_MissingAuthorInfo_ShouldMapUnknownAuthor()
        {
            var blogs = new List<Blog> { new Blog { Id = "b1", AuthorId = "unknown-id", Tags = new List<Tag>() } };
            var pagedData = new PagedResult<Blog>(blogs, 1, 1, 10);
            
            _blogRepoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>()))
                            .ReturnsAsync(new Dictionary<string, AccountBasicInfoDTO>()); // Missing in dictionary

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().Author.FullName.Should().Be("Người dùng ẩn danh");

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-02",
                Description = "Maps missing author IDs to default fallback string",
                ExpectedResult = "'Người dùng ẩn danh'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AuthorId missing in Account mapping" }
            });
        }

        // TC-BLG-GPB-03 | N | Valid Author mapped
        [Fact]
        public async Task Handle_ValidAuthorInfo_ShouldMapRealAuthorName()
        {
            var blogs = new List<Blog> { new Blog { Id = "b1", AuthorId = "valid-id", Tags = new List<Tag>() } };
            var pagedData = new PagedResult<Blog>(blogs, 1, 1, 10);
            
            _blogRepoMock.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<BlogStatus?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
                         
            var dic = new Dictionary<string, AccountBasicInfoDTO>
            {
                { "valid-id", new AccountBasicInfoDTO { FullName = "Nguyễn A", AvatarUrl = "url" } }
            };
            
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>())).ReturnsAsync(dic);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            var author = result.Data!.Items.First().Author;
            author.FullName.Should().Be("Nguyễn A");
            author.AvatarUrl.Should().Be("url");

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-03",
                Description = "Exact AccountInfo mapped properly",
                ExpectedResult = "'Nguyễn A' mapping",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Author exists in Dictionary" }
            });
        }

        // TC-BLG-GPB-04 | N | Category name defaults correctly
        [Fact]
        public async Task Handle_MissingCategoryName_ShouldMapToDefaultString()
        {
            // Blog omitting Category navigation
            var blogs = new List<Blog> { new Blog { Id = "b1", Category = null, Tags = new List<Tag>() } };
            var pagedData = new PagedResult<Blog>(blogs, 1, 1, 10);
            
            _blogRepoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>()))
                            .ReturnsAsync(new Dictionary<string, AccountBasicInfoDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            result.Data!.Items.First().CategoryName.Should().Be("Không xác định");

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-04",
                Description = "Null reference on Category navigation property fallback verified",
                ExpectedResult = "'Không xác định'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Category property is null" }
            });
        }

        // TC-BLG-GPB-05 | N | Tag Projection mapped cleanly
        [Fact]
        public async Task Handle_ProperTagsData_ShouldMapListStrictly()
        {
            var tags = new List<Tag> { new Tag { Name = "C#" }, new Tag { Name = ".NET" } };
            var blogs = new List<Blog> { new Blog { Id = "b1", Category = new Category { Name = "Tech" }, Tags = tags } };
            var pagedData = new PagedResult<Blog>(blogs, 1, 1, 10);
            
            _blogRepoMock.Setup(x => x.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>()))
                            .ReturnsAsync(new Dictionary<string, AccountBasicInfoDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            var resultTags = result.Data!.Items.First().Tags;
            resultTags.Should().BeEquivalentTo(new[] { "C#", ".NET" });

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-05",
                Description = "Tags relation cleanly flattened into string List using LINQ",
                ExpectedResult = "List matches { 'C#', '.NET' }",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Tags Count = 2" }
            });
        }

        // TC-BLG-GPB-06 | B | Pagination Propagation validated
        [Fact]
        public async Task Handle_PaginationProps_ShouldMapAccuratelyToResult()
        {
            var pagedData = new PagedResult<Blog>(new List<Blog>(), 99, 5, 20); // Extracted properties
            _blogRepoMock.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<BlogStatus?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pagedData);
            _accountRepoMock.Setup(x => x.GetBasicInfosAsync(It.IsAny<List<string>>()))
                            .ReturnsAsync(new Dictionary<string, AccountBasicInfoDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new GetPagedBlogsQuery { PageNumber = 5, PageSize = 20 }, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(99);
            result.Data.PageNumber.Should().Be(5);
            result.Data.PageSize.Should().Be(20);

            QACollector.LogTestCase("Blog - Get Paged", new TestCaseDetail
            {
                FunctionGroup = "GetPagedBlogsQueryHandler",
                TestCaseID = "TC-BLG-GPB-06",
                Description = "DTO properties initialized properly bridging metadata",
                ExpectedResult = "Metadata fully matches constructor injection",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Page metadata asserts" }
            });
        }
    }
}
