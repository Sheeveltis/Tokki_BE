using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.FavoriteVocabulary.DTOs;
using Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.FavoriteVocabulary
{
    public class GetFavoriteVocabulariesQueryHandlerTests
    {
        private static GetFavoriteVocabulariesQueryHandler CreateHandler(
            Mock<IUserFavoriteVocabularyRepository>? favRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new GetFavoriteVocabulariesQueryHandler(
                (favRepo   ?? new Mock<IUserFavoriteVocabularyRepository>()).Object,
                (topicRepo ?? new Mock<ITopicRepository>()).Object,
                (httpCtx   ?? BuildHttpContext("USER-001")).Object);
        }

        private static Mock<IHttpContextAccessor> BuildHttpContext(string? userId)
        {
            var mock = new Mock<IHttpContextAccessor>();
            if (userId == null)
            {
                mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var claims   = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
                var identity  = new ClaimsIdentity(claims);
                var principal = new ClaimsPrincipal(identity);
                var ctx       = new DefaultHttpContext { User = principal };
                mock.Setup(x => x.HttpContext).Returns(ctx);
            }
            return mock;
        }

        private static GetFavoriteVocabulariesQuery DefaultQuery => new()
        {
            PageNumber = 1, PageSize = 10
        };

        private static UserFavoriteVocabulary BuildFavItem(string vocabId) => new()
        {
            FavoriteVocabularyId = $"FAV-{vocabId}",
            UserId       = "USER-001",
            VocabularyId = vocabId,
            CreatedAt    = DateTime.UtcNow,
            Vocabulary   = new Tokki.Domain.Entities.Vocabulary
            {
                VocabularyId  = vocabId,
                Text          = $"Word-{vocabId}",
                Definition    = "def",
                Pronunciation = "pron"
            }
        };

        // TC-01: No user → 401
        [Fact]
        public async Task Handle_NoUserId_ShouldReturn401()
        {
            var httpCtx = BuildHttpContext(null);
            var result  = await CreateHandler(httpCtx: httpCtx)
                .Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-01",
                Description = "No user context → 401 Unauthorized",
                ExpectedResult = "Return 401", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userId == null => 401" }
            });
        }

        // TC-02: TopicId provided but topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var topicRepo = new Mock<ITopicRepository>();
            topicRepo.Setup(x => x.GetByIdAsync("TOPIC-999")).ReturnsAsync((Topic?)null);

            var query  = new GetFavoriteVocabulariesQuery { PageNumber = 1, PageSize = 10, TopicId = "TOPIC-999" };
            var result = await CreateHandler(topicRepo: topicRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-02",
                Description = "TopicId provided but topic not found → 404",
                ExpectedResult = "Return 404 TopicNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "topicId != null && topic == null => 404" }
            });
        }

        // TC-03: Topic inactive → 404
        [Fact]
        public async Task Handle_TopicInactive_ShouldReturn404()
        {
            var topicRepo = new Mock<ITopicRepository>();
            topicRepo.Setup(x => x.GetByIdAsync("TOPIC-001"))
                     .ReturnsAsync(new Topic { TopicId = "TOPIC-001", Status = TopicStatus.Draft });

            var query  = new GetFavoriteVocabulariesQuery { PageNumber = 1, PageSize = 10, TopicId = "TOPIC-001" };
            var result = await CreateHandler(topicRepo: topicRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-03",
                Description = "Topic exists but Disabled → 404",
                ExpectedResult = "Return 404 TopicNotFound", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "topic.Status != Active => 404" }
            });
        }

        // TC-04: Empty favorites → 200 empty paged
        [Fact]
        public async Task Handle_EmptyFavorites_ShouldReturnEmptyPaged()
        {
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.GetPagedByUserAndTopicAsync(
                "USER-001", null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new List<UserFavoriteVocabulary>(), 0));

            var result = await CreateHandler(favRepo: favRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().BeEmpty();

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-04",
                Description = "No favorites → Return 200 empty paged",
                ExpectedResult = "Return 200, Items=[]", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "totalCount == 0 => empty page" }
            });
        }

        // TC-05: Happy path → DTOs mapped correctly
        [Fact]
        public async Task Handle_WithFavorites_ShouldMapDtosCorrectly()
        {
            var items = new List<UserFavoriteVocabulary> { BuildFavItem("V-001"), BuildFavItem("V-002") };
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.GetPagedByUserAndTopicAsync(
                "USER-001", null, 1, 10, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((items, 2));

            var result = await CreateHandler(favRepo: favRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].VocabularyId.Should().Be("V-001");
            result.Data.Items[0].Text.Should().Be("Word-V-001");

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-05",
                Description = "2 favorites → DTOs mapped with correct fields",
                ExpectedResult = "Return 200, Items=2, DTO fields correct", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "items mapped to FavoriteVocabularyDto" }
            });
        }

        // TC-06: Pagination applied correctly
        [Fact]
        public async Task Handle_Pagination_ShouldReturnCorrectPageMetadata()
        {
            var items = new List<UserFavoriteVocabulary> { BuildFavItem("V-001") };
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.GetPagedByUserAndTopicAsync(
                "USER-001", null, 2, 5, null, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((items, 15));

            var query  = new GetFavoriteVocabulariesQuery { PageNumber = 2, PageSize = 5 };
            var result = await CreateHandler(favRepo: favRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalCount.Should().Be(15);
            result.Data.TotalPages.Should().Be(3);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Favorite Vocabulary - Get", new TestCaseDetail
            {
                FunctionGroup = "GetFavoriteVocabularies", TestCaseID = "TC-FAV-GET-06",
                Description = "Page 2 of 15 with PageSize=5 → TotalPages=3",
                ExpectedResult = "TotalPages=3, CurrentPage=2", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PagedResult.Create(page=2, size=5, total=15)" }
            });
        }
    }
}
