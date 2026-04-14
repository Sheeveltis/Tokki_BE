using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.DTOs;
using Tokki.Application.UseCases.Passages.Queries.GetPassages;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Passages
{
    public class GetPassagesQueryHandlerTests
    {
        private static GetPassagesQueryHandler CreateHandler(
            Mock<IPassageRepository>? passageRepo = null)
        {
            return new GetPassagesQueryHandler(
                (passageRepo ?? new Mock<IPassageRepository>()).Object);
        }

        private static GetPassagesQuery DefaultQuery => new()
        {
            PageNumber = 1, PageSize = 10
        };

        private static Passage BuildPassage(string id, PassageMediaType type = PassageMediaType.Text) => new()
        {
            PassageId = id,
            Title     = $"Passage {id}",
            Content   = "Content",
            MediaType = type,
            Status    = PassageStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // TC-01: Empty result → 200 empty paged
        [Fact]
        public async Task Handle_NoPassages_ShouldReturnEmptyPaged()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Passage>(), 0));

            var result = await CreateHandler(repo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-01",
                Description = "No passages exist → 200 empty paged result",
                ExpectedResult = "Return 200, Items=[]", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "totalCount == 0 => empty paged" }
            });
        }

        // TC-02: Happy path → DTOs mapped correctly
        [Fact]
        public async Task Handle_WithPassages_ShouldMapDtosCorrectly()
        {
            var items = new List<Passage>
            {
                BuildPassage("P-001", PassageMediaType.Text),
                BuildPassage("P-002", PassageMediaType.Image)
            };
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(1, 10, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items, 2));

            var result = await CreateHandler(repo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].PassageId.Should().Be("P-001");
            result.Data.Items[1].MediaType.Should().Be(PassageMediaType.Image);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-02",
                Description = "2 passages → DTOs mapped with correct PassageId and MediaType",
                ExpectedResult = "Return 200, Items.Count=2", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passages mapped to PassageDto" }
            });
        }

        // TC-03: Filter by MediaType
        [Fact]
        public async Task Handle_FilterByMediaType_ShouldPassFilterToRepo()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(1, 10, null, PassageMediaType.Audio, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Passage> { BuildPassage("P-003", PassageMediaType.Audio) }, 1));

            var query  = new GetPassagesQuery { PageNumber = 1, PageSize = 10, MediaType = PassageMediaType.Audio };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            repo.Verify(x => x.GetPagedAsync(1, 10, null, PassageMediaType.Audio, null, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-03",
                Description = "Filter by MediaType=Audio → Repo called with Audio filter",
                ExpectedResult = "Return 200, 1 Audio passage", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "MediaType filter passed to repo" }
            });
        }

        // TC-04: Filter by Status
        [Fact]
        public async Task Handle_FilterByStatus_ShouldPassStatusToRepo()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(1, 10, null, null, PassageStatus.Hidden, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Passage>(), 0));

            var query  = new GetPassagesQuery { PageNumber = 1, PageSize = 10, Status = PassageStatus.Hidden };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.GetPagedAsync(1, 10, null, null, PassageStatus.Hidden, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-04",
                Description = "Filter by Status=Hidden → Repo called with Hidden status",
                ExpectedResult = "Return 200, repo called with Hidden", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status filter passed to repo" }
            });
        }

        // TC-05: SearchTerm passed through
        [Fact]
        public async Task Handle_WithSearchTerm_ShouldPassToRepo()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(1, 10, "Korean", null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Passage> { BuildPassage("P-005") }, 1));

            var query  = new GetPassagesQuery { PageNumber = 1, PageSize = 10, SearchTerm = "Korean" };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            repo.Verify(x => x.GetPagedAsync(1, 10, "Korean", null, null, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-05",
                Description = "SearchTerm='Korean' → passed to repo, return matched results",
                ExpectedResult = "Return 200, 1 result", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "searchTerm passed to GetPagedAsync" }
            });
        }

        // TC-06: Pagination metadata correct
        [Fact]
        public async Task Handle_Pagination_ShouldReturnCorrectPageMetadata()
        {
            var items = new List<Passage> { BuildPassage("P-001"), BuildPassage("P-002") };
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetPagedAsync(2, 5, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items, 12));

            var query  = new GetPassagesQuery { PageNumber = 2, PageSize = 5 };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalCount.Should().Be(12);
            result.Data.TotalPages.Should().Be(3);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Passage - Get List", new TestCaseDetail
            {
                FunctionGroup = "GetPassages", TestCaseID = "TC-PAS-LST-06",
                Description = "Page 2 of 12, PageSize=5 → TotalPages=3, CurrentPage=2",
                ExpectedResult = "TotalPages=3, CurrentPage=2, TotalCount=12", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Paged metadata correct" }
            });
        }
    }
}
