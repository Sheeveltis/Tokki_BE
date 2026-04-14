using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.Queries.CheckUserPlayedLevel;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Games.Queries
{
    public class CheckUserPlayedLevelQueryHandlerTests
    {
        private readonly Mock<IGameMatchSessionRepository> _sessionMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<CheckUserPlayedLevelQueryHandler>> _loggerMock = new();

        private CheckUserPlayedLevelQueryHandler CreateHandler()
        {
            return new CheckUserPlayedLevelQueryHandler(_sessionMock.Object, _httpMock.Object, _loggerMock.Object);
        }

        private void SetupHttpContext(string? nameIdentifier)
        {
            if (nameIdentifier == null)
            {
                _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, nameIdentifier) });
            context.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(context);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-01 | A | Missing HttpContext completely -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullHttpContext_ShouldReturn401()
        {
            SetupHttpContext(null); // No token
            var handler = CreateHandler();
            var cmd = new CheckUserPlayedLevelQuery();

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-01",
                Description = "Security catches null http context context correctly unauthorized",
                ExpectedResult = "401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HTTP Context Null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-02 | A | Missing UserId Claim explicitly -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyUserId_ShouldReturn401()
        {
            SetupHttpContext(""); // Blank claim
            var handler = CreateHandler();
            var cmd = new CheckUserPlayedLevelQuery();

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-02",
                Description = "Empty user identity string blocked securely securely natively",
                ExpectedResult = "401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "NameIdentifier explicitly empty string" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-03 | A | Repo throws Exception -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            SetupHttpContext("usr"); 
            _sessionMock.Setup(x => x.GetByUserGameTopicAsync("usr", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tokki.Domain.Enums.GameDifficulty>()))
                        .ThrowsAsync(new Exception("DB Timeout"));

            var handler = CreateHandler();
            var result = await handler.Handle(new CheckUserPlayedLevelQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-03",
                Description = "Catch block smoothly identifies SQL connection timeout or error resolving 500 safely",
                ExpectedResult = "500 Internal Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception triggered inside try block" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-04 | N | Session Found -> Returns True
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionExists_ShouldReturnTrue()
        {
            SetupHttpContext("usr"); 
            _sessionMock.Setup(x => x.GetByUserGameTopicAsync("usr", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tokki.Domain.Enums.GameDifficulty>()))
                        .ReturnsAsync(new GameMatchSession());

            var handler = CreateHandler();
            var result = await handler.Handle(new CheckUserPlayedLevelQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-04",
                Description = "Item verified resolving Boolean TRUE internally dynamically",
                ExpectedResult = "Return Data = True",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session matched correctly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-05 | N | Session NotFound -> Returns False
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNull_ShouldReturnFalse()
        {
            SetupHttpContext("usr"); 
            _sessionMock.Setup(x => x.GetByUserGameTopicAsync("usr", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tokki.Domain.Enums.GameDifficulty>()))
                        .ReturnsAsync((GameMatchSession?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(new CheckUserPlayedLevelQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); // Still successful HTTP Status 200
            result.Data.Should().BeFalse(); // Value Data is false

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-05",
                Description = "Returns FALSE directly avoiding exceptions natively",
                ExpectedResult = "Return Data = False",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null matched securely" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAM-CPL-06 | B | Claim Extraction Overrides Command Input DTO accurately
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ClaimOverridesDTO_ShouldMapContext()
        {
            SetupHttpContext("secure-user-claim"); 
            _sessionMock.Setup(x => x.GetByUserGameTopicAsync("secure-user-claim", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tokki.Domain.Enums.GameDifficulty>()))
                        .ReturnsAsync((GameMatchSession?)null);

            var handler = CreateHandler();
            // User sent custom hacked param, but it should override securely
            var cmd = new CheckUserPlayedLevelQuery { UserId = "hacked-other-user" }; 
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Verify mock was hit with "secure-user-claim", proving it overwrote parameter with HttpContext
            _sessionMock.Verify(x => x.GetByUserGameTopicAsync("secure-user-claim", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tokki.Domain.Enums.GameDifficulty>()), Times.Once);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup = "CheckUserPlayedLevelQueryHandler",
                TestCaseID = "TC-GAM-CPL-06",
                Description = "Identity strictly overwrites arbitrary property payloads maliciously injected bypassing limits securely",
                ExpectedResult = "Valid mapping executed overriding param",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Input userId modified strictly by Token rules" }
            });
        }
    }
}
