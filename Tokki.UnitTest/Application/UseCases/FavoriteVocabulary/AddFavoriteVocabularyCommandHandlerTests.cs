using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.FavoriteVocabulary
{
    public class AddFavoriteVocabularyCommandHandlerTests
    {
        private static AddFavoriteVocabularyCommandHandler CreateHandler(
            Mock<IUserFavoriteVocabularyRepository>? favRepo = null,
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            return new AddFavoriteVocabularyCommandHandler(
                (favRepo  ?? new Mock<IUserFavoriteVocabularyRepository>()).Object,
                (vocabRepo ?? new Mock<IVocabularyRepository>()).Object,
                (httpCtx  ?? new Mock<IHttpContextAccessor>()).Object,
                (idGen    ?? new Mock<IIdGeneratorService>()).Object);
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
                var claims  = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
                var identity = new ClaimsIdentity(claims);
                var principal = new ClaimsPrincipal(identity);
                var ctx = new DefaultHttpContext { User = principal };
                mock.Setup(x => x.HttpContext).Returns(ctx);
            }
            return mock;
        }

        private static Tokki.Domain.Entities.Vocabulary ActiveVocab(string id) => new()
        {
            VocabularyId = id,
            Text         = "안녕",
            Status       = VocabularyStatus.Active
        };

        // TC-01: No user in context → 401
        [Fact]
        public async Task Handle_NoUserId_ShouldReturn401()
        {
            var httpCtx = BuildHttpContext(null);
            var result  = await CreateHandler(httpCtx: httpCtx)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-01",
                Description = "No user context → 401 Unauthorized",
                ExpectedResult = "Return 401 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userId == null => 401" }
            });
        }

        // TC-02: Vocab not found → 404
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var httpCtx   = BuildHttpContext("USER-001");
            var vocabRepo = new Mock<IVocabularyRepository>();
            vocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var result = await CreateHandler(httpCtx: httpCtx, vocabRepo: vocabRepo)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "INVALID" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-02",
                Description = "VocabularyId not found → 404",
                ExpectedResult = "Return 404 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vocab == null => 404" }
            });
        }

        // TC-03: Vocab inactive → 404
        [Fact]
        public async Task Handle_VocabInactive_ShouldReturn404()
        {
            var httpCtx   = BuildHttpContext("USER-001");
            var vocabRepo = new Mock<IVocabularyRepository>();
            vocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { new() { VocabularyId = "V-001", Status = VocabularyStatus.Draft } });

            var result = await CreateHandler(httpCtx: httpCtx, vocabRepo: vocabRepo)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-03",
                Description = "Vocab is Pending (not Active) → 404",
                ExpectedResult = "Return 404 Failure", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vocab.Status != Active => 404" }
            });
        }

        // TC-04: Already exists → idempotent success 200
        [Fact]
        public async Task Handle_AlreadyFavorited_ShouldReturnIdempotentSuccess()
        {
            var httpCtx   = BuildHttpContext("USER-001");
            var vocabRepo = new Mock<IVocabularyRepository>();
            vocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { ActiveVocab("V-001") });

            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.ExistsAsync("USER-001", "V-001", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(favRepo: favRepo, httpCtx: httpCtx, vocabRepo: vocabRepo)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            favRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-04",
                Description = "Already favorited → idempotent 200 without re-inserting",
                ExpectedResult = "Return 200, AddAsync NOT called", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "exists == true => 200 no-op" }
            });
        }

        // TC-05: Happy path → 200
        [Fact]
        public async Task Handle_ValidNewFavorite_ShouldReturn200AndAdd()
        {
            var httpCtx   = BuildHttpContext("USER-001");
            var vocabRepo = new Mock<IVocabularyRepository>();
            vocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { ActiveVocab("V-001") });

            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.ExistsAsync("USER-001", "V-001", It.IsAny<CancellationToken>())).ReturnsAsync(false);
            favRepo.Setup(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var idGen = new Mock<IIdGeneratorService>();
            idGen.Setup(x => x.GenerateCustom(15)).Returns("FAVID-001");

            var result = await CreateHandler(favRepo: favRepo, vocabRepo: vocabRepo, httpCtx: httpCtx, idGen: idGen)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            favRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-05",
                Description = "Valid new favorite → AddAsync called, Return 200",
                ExpectedResult = "Return 200, AddAsync called once", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "exists == false => AddAsync => 200" }
            });
        }

        // TC-06: Repository throws → 400
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn400()
        {
            var httpCtx   = BuildHttpContext("USER-001");
            var vocabRepo = new Mock<IVocabularyRepository>();
            vocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { ActiveVocab("V-001") });

            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            favRepo.Setup(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new Exception("DB error"));

            var idGen = new Mock<IIdGeneratorService>();
            idGen.Setup(x => x.GenerateCustom(15)).Returns("FAV-001");

            var result = await CreateHandler(favRepo: favRepo, vocabRepo: vocabRepo, httpCtx: httpCtx, idGen: idGen)
                .Handle(new AddFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Favorite Vocabulary - Add", new TestCaseDetail
            {
                FunctionGroup = "AddFavoriteVocabulary", TestCaseID = "TC-FAV-ADD-06",
                Description = "AddAsync throws (race condition/DB error) → 400 Failure",
                ExpectedResult = "Return 400 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws => catch => 400" }
            });
        }
    }
}
