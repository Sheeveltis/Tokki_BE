using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.RefreshToken;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtMock = new();

        private RefreshTokenCommandHandler CreateHandler()
        {
            return new RefreshTokenCommandHandler(_refreshTokenServiceMock.Object, _jwtMock.Object);
        }

        // RefreshTokenCommandHandler_01 | N | Valid Token with Null Avatar -> Default avatar assigned
        [Fact]
        public async Task Handle_NullAvatar_ShouldUseDefaultAvatar()
        {
            var user = new Account { UserId = "u1", AvatarUrl = null, FullName = "Test", Role = AccountRole.User };
            var oldToken = new RefreshToken { TokenHash = "hash", User = user };
            
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw-token")).ReturnsAsync(oldToken);
            _refreshTokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(oldToken)).ReturnsAsync("new-raw");
            _jwtMock.Setup(x => x.GenerateToken(user, It.IsAny<DateTime>())).Returns("new-jwt");

            var handler = CreateHandler();
            var result = await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw-token" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.AvatarUrl.Should().Be("default-avatar");

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_01",
                Description = "Null Avatar correctly outputs default-avatar",
                ExpectedResult = "default-avatar string returned",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "AvatarUrl is null" }
            });
        }

        // RefreshTokenCommandHandler_02 | N | Valid Token with Has Avatar -> Actual avatar assigned
        [Fact]
        public async Task Handle_HasAvatar_ShouldUseRealAvatar()
        {
            var user = new Account { UserId = "u1", AvatarUrl = "https://img.com/a.jpg", FullName = "Test", Role = AccountRole.User };
            var oldToken = new RefreshToken { TokenHash = "hash", User = user };
            
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw")).ReturnsAsync(oldToken);
            _refreshTokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(oldToken)).ReturnsAsync("new-raw");
            _jwtMock.Setup(x => x.GenerateToken(user, It.IsAny<DateTime>())).Returns("new-jwt");

            var handler = CreateHandler();
            var result = await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.AvatarUrl.Should().Be("https://img.com/a.jpg");

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_02",
                Description = "Valid Avatar is preserved directly in output",
                ExpectedResult = "Actual AvatarUrl returned",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "AvatarUrl has value" }
            });
        }

        // RefreshTokenCommandHandler_03 | A | Verify Throws Exception -> Bubbles up
        [Fact]
        public async Task Handle_VerifyThrows_ShouldBubbleUp()
        {
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw"))
                                    .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

            var handler = CreateHandler();
            
            var act = async () => await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw" }, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid token");

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_03",
                Description = "Verify exception is bubbled up un-intercepted",
                ExpectedResult = "Throws Exception",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Service throws" }
            });
        }

        // RefreshTokenCommandHandler_04 | B | Verify Rotate is called with old token
        [Fact]
        public async Task Handle_ShouldCallRotateWithExactOldToken()
        {
            var user = new Account { UserId = "u1", AvatarUrl = null, FullName = "Test", Role = AccountRole.User };
            var oldToken = new RefreshToken { TokenHash = "hash123", User = user };
            
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw")).ReturnsAsync(oldToken);
            _refreshTokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(oldToken)).ReturnsAsync("new-raw");

            var handler = CreateHandler();
            await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw" }, CancellationToken.None);

            _refreshTokenServiceMock.Verify(x => x.RotateRefreshTokenAsync(It.Is<RefreshToken>(t => t.TokenHash == "hash123")), Times.Once);

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_04",
                Description = "Output depends on exact Rotation of validated token",
                ExpectedResult = "RotateRefreshTokenAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Mock Verification" }
            });
        }

        // RefreshTokenCommandHandler_05 | B | Request checks expiration adds 60 mins
        [Fact]
        public async Task Handle_GenerateToken_ShouldAdd60MinsExpire()
        {
            var user = new Account { UserId = "u1", AvatarUrl = null, FullName = "Test", Role = AccountRole.User };
            var oldToken = new RefreshToken { TokenHash = "hash123", User = user };
            
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw")).ReturnsAsync(oldToken);
            
            DateTime capturedDate = default;
            _jwtMock.Setup(x => x.GenerateToken(user, It.IsAny<DateTime>()))
                    .Callback<Account, DateTime>((u, d) => capturedDate = d)
                    .Returns("new-jwt");

            var handler = CreateHandler();
            var startTime = DateTime.UtcNow;
            await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw" }, CancellationToken.None);

            // Add 60 mins diff assert
            capturedDate.Should().BeCloseTo(startTime.AddMinutes(60), TimeSpan.FromSeconds(5));

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_05",
                Description = "Ensure Token is requested with correct Expiration window (60m)",
                ExpectedResult = "Date generated within tolerance",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Check TimeSpan boundaries" }
            });
        }

        // RefreshTokenCommandHandler_06 | N | Verify Login Response Mapping 
        [Fact]
        public async Task Handle_Success_ShouldMapLoginResponsePerfectly()
        {
            var user = new Account { UserId = "u1", AvatarUrl = "v", FullName = "MyName D", Role = AccountRole.Admin };
            var oldToken = new RefreshToken { TokenHash = "h", User = user };
            
            _refreshTokenServiceMock.Setup(x => x.VerifyRefreshTokenAsync("raw")).ReturnsAsync(oldToken);
            _refreshTokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(oldToken)).ReturnsAsync("new-ref");
            _jwtMock.Setup(x => x.GenerateToken(user, It.IsAny<DateTime>())).Returns("new-jwt");

            var handler = CreateHandler();
            var result = await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "raw" }, CancellationToken.None);

            result.Data!.Token.Should().Be("new-jwt");
            result.Data.RefreshToken.Should().Be("new-ref");
            result.Data.FullName.Should().Be("MyName D");
            result.Data.Role.Should().Be("Admin");

            QACollector.LogTestCase("Account - Refresh Token", new TestCaseDetail
            {
                FunctionGroup = "RefreshTokenCommandHandler",
                TestCaseID = "RefreshTokenCommandHandler_06",
                Description = "Correctly maps LoginResponse properties like Roles",
                ExpectedResult = "All DTO fields matched",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Verify Full Flow" }
            });
        }
    }
}
