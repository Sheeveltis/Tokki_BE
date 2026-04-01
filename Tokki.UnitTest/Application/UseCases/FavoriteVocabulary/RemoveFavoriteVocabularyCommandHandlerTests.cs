using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary;
using Tokki.Application.Common.Models;
using Tokki.UnitTest.Utilities;
using Tokki.Application.UseCases.UserFavoriteVocabularies.Commands.RemoveFavoriteVocabulary;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.FavoriteVocabulary
{
    public class RemoveFavoriteVocabularyCommandHandlerTests
    {
        private static RemoveFavoriteVocabularyCommandHandler CreateHandler(
            Mock<IUserFavoriteVocabularyRepository>? favRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null,
            Mock<IValidator<RemoveFavoriteVocabularyCommand>>? validator = null)
        {
            var mockValidator = validator ?? BuildPassValidator();
            return new RemoveFavoriteVocabularyCommandHandler(
                (favRepo ?? new Mock<IUserFavoriteVocabularyRepository>()).Object,
                (httpCtx ?? BuildHttpContext("USER-001")).Object,
                mockValidator.Object);
        }

        private static Mock<IValidator<RemoveFavoriteVocabularyCommand>> BuildPassValidator()
        {
            var v = new Mock<IValidator<RemoveFavoriteVocabularyCommand>>();
            v.Setup(x => x.ValidateAsync(It.IsAny<RemoveFavoriteVocabularyCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult());
            return v;
        }

        private static Mock<IValidator<RemoveFavoriteVocabularyCommand>> BuildFailValidator()
        {
            var v = new Mock<IValidator<RemoveFavoriteVocabularyCommand>>();
            v.Setup(x => x.ValidateAsync(It.IsAny<RemoveFavoriteVocabularyCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("VocabularyId", "Required") }));
            return v;
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

        // TC-01: Validation fails → 400
        [Fact]
        public async Task Handle_ValidationFails_ShouldReturn400()
        {
            var result = await CreateHandler(validator: BuildFailValidator())
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-01",
                Description = "Validator fails → 400 ValidationFailed",
                ExpectedResult = "Return 400 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "validationResult.IsValid == false => 400" }
            });
        }

        // TC-02: No user in context → 401
        [Fact]
        public async Task Handle_NoUserId_ShouldReturn401()
        {
            var httpCtx = BuildHttpContext(null);
            var result  = await CreateHandler(httpCtx: httpCtx)
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-02",
                Description = "No user in HttpContext → 401",
                ExpectedResult = "Return 401 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userId == null => 401" }
            });
        }

        // TC-03: Item not in favorites (deleted == 0) → idempotent 200
        [Fact]
        public async Task Handle_NotInFavorites_ShouldReturnIdempotentSuccess()
        {
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.HardDeleteAsync("USER-001", "V-001", It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var result = await CreateHandler(favRepo: favRepo)
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-03",
                Description = "Vocab not in favorites (deleted==0) → idempotent 200",
                ExpectedResult = "Return 200 Success", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "deleted == 0 => 200 idempotent" }
            });
        }

        // TC-04: Happy path → deleted > 0 → 200
        [Fact]
        public async Task Handle_ValidRemove_ShouldReturn200()
        {
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.HardDeleteAsync("USER-001", "V-001", It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await CreateHandler(favRepo: favRepo)
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-04",
                Description = "Valid remove → HardDeleteAsync called, Return 200",
                ExpectedResult = "Return 200, deleted=1", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "deleted > 0 => 200 success" }
            });
        }

        // TC-05: HardDeleteAsync throws → 400
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn400()
        {
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.HardDeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new Exception("DB error"));

            var result = await CreateHandler(favRepo: favRepo)
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "V-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-05",
                Description = "HardDeleteAsync throws → catch → 400",
                ExpectedResult = "Return 400 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HardDeleteAsync throws => catch => 400" }
            });
        }

        // TC-06: Verify HardDeleteAsync called with correct userId and vocabId
        [Fact]
        public async Task Handle_VerifiesCorrectUserAndVocab()
        {
            var favRepo = new Mock<IUserFavoriteVocabularyRepository>();
            favRepo.Setup(x => x.HardDeleteAsync("USER-001", "V-SPECIFIC", It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await CreateHandler(favRepo: favRepo)
                .Handle(new RemoveFavoriteVocabularyCommand { VocabularyId = "V-SPECIFIC" }, CancellationToken.None);

            favRepo.Verify(x => x.HardDeleteAsync("USER-001", "V-SPECIFIC", It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Favorite Vocabulary - Remove", new TestCaseDetail
            {
                FunctionGroup = "RemoveFavoriteVocabulary", TestCaseID = "TC-FAV-REM-06",
                Description = "HardDeleteAsync called with exact userId and vocabId",
                ExpectedResult = "HardDeleteAsync('USER-001','V-SPECIFIC') called once", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HardDeleteAsync(userId, vocabId) called once" }
            });
        }
    }
}
