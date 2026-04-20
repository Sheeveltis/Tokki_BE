using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class LoginCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private LoginCommandHandler CreateHandler(
            Mock<IAccountRepository>?     accountRepo      = null,
            Mock<ISystemConfigRepository>? configRepo      = null,
            Mock<IJwtTokenGenerator>?      jwtGen          = null,
            Mock<IGamificationService>?    gamification    = null,
            Mock<IEmailHistoryRepository>? emailHistoryRepo = null,
            Mock<IRefreshTokenService>?    refreshTokenSvc = null)
        {
            var mockJwt = jwtGen ?? new Mock<IJwtTokenGenerator>();
            mockJwt.Setup(x => x.GenerateToken(It.IsAny<Account>(), It.IsAny<DateTime>()))
                   .Returns("fake-jwt-token");

            return new LoginCommandHandler(
                (accountRepo      ?? MockAccountRepository.GetMock()).Object,
                (configRepo       ?? BuildDefaultConfigMock()).Object,
                mockJwt.Object,
                MockIdGeneratorService.GetMock().Object,
                (gamification     ?? BuildGamificationMock()).Object,
                new Mock<IValidator<LoginCommand>>().Object,
                (emailHistoryRepo  ?? BuildEmailHistoryMock()).Object,
                (refreshTokenSvc   ?? BuildRefreshTokenMock()).Object);
        }

        // ── Shared builders ──────────────────────────────────────────
        private static Account ActiveUser(string password = "ValidPass123!") => new()
        {
            UserId            = "USER-001",
            Email             = "user@tokki.com",
            FullName          = "Test User",
            Role              = AccountRole.User,
            Status            = AccountStatus.Active,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(password),
            FailedLoginCount  = 0
        };

        private static Mock<ISystemConfigRepository> BuildDefaultConfigMock(
            string failedLimit  = "5",
            string lockLevel1   = "300",
            string lockLevel2   = "1800",
            string lockLevel3   = "PERMANENT_LOCK",
            string tokenExp     = "60",
            string defaultStaff = "StaffDefaultPass",
            string defaultUser  = "UserDefaultPass",
            string defaultAdmin = "AdminDefaultPass")
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT")).ReturnsAsync(failedLimit);
            m.Setup(x => x.GetValueByKeyAsync("LOGIN_LOCKOUT_DURATION_LEVEL_1")).ReturnsAsync(lockLevel1);
            m.Setup(x => x.GetValueByKeyAsync("LOGIN_LOCKOUT_DURATION_LEVEL_2")).ReturnsAsync(lockLevel2);
            m.Setup(x => x.GetValueByKeyAsync("LOGIN_LOCKOUT_LEVEL_3_ACTION")).ReturnsAsync(lockLevel3);
            m.Setup(x => x.GetValueByKeyAsync("TOKEN_EXPIRATION_MINUTES")).ReturnsAsync(tokenExp);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF")).ReturnsAsync(defaultStaff);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync(defaultUser);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN")).ReturnsAsync(defaultAdmin);
            return m;
        }

        private static Mock<IGamificationService> BuildGamificationMock()
        {
            var m = new Mock<IGamificationService>();
            m.Setup(x => x.CheckLoginGamificationAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IEmailHistoryRepository> BuildEmailHistoryMock()
        {
            var m = new Mock<IEmailHistoryRepository>();
            m.Setup(x => x.DeleteByUserAndTemplateTypeAsync(
                It.IsAny<string>(), It.IsAny<EmailTemplateType>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IRefreshTokenService> BuildRefreshTokenMock()
        {
            var m = new Mock<IRefreshTokenService>();
            m.Setup(x => x.RevokeAllRefreshTokensAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            m.Setup(x => x.CreateRefreshTokenAsync(It.IsAny<Account>())).ReturnsAsync("fake-refresh-token");
            return m;
        }

        private static Mock<IAccountRepository> BuildAccountRepoWith(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // Login_01 | A | Email not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var result = await CreateHandler()
                .Handle(new LoginCommand { Email = "ghost@tokki.com", Password = "Abc123" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_01",
                Description     = "Email does not exist in the system",
                ExpectedResult  = "Return 404 UserNotFound",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByEmailAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_02 | A | Account Inactive → 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountInactive_ShouldReturn403()
        {
            var user = ActiveUser(); user.Status = AccountStatus.Inactive;
            var result = await CreateHandler(accountRepo: BuildAccountRepoWith(user))
                .Handle(new LoginCommand { Email = user.Email, Password = "ValidPass123!" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_02",
                Description     = "Account status is Inactive",
                ExpectedResult  = "Return 403 AccountInActive",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccountStatus = Inactive", "Return 403" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_03 | A | Account Banned → 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountBanned_ShouldReturn403()
        {
            var user = ActiveUser(); user.Status = AccountStatus.Banned;
            var result = await CreateHandler(accountRepo: BuildAccountRepoWith(user))
                .Handle(new LoginCommand { Email = user.Email, Password = "ValidPass123!" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_03",
                Description     = "Account is permanently banned",
                ExpectedResult  = "Return 403 AccountBanned",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccountStatus = Banned", "Return 403" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_04 | A | Account temporarily locked → 403 with remaining minutes
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountLockedUntilFuture_ShouldReturn403WithRemainingMinutes()
        {
            var user = ActiveUser();
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(15);

            var result = await CreateHandler(accountRepo: BuildAccountRepoWith(user))
                .Handle(new LoginCommand { Email = user.Email, Password = "ValidPass123!" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("phút");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_04",
                Description     = "Account is temporarily locked, LockedUntil is 15 minutes in the future",
                ExpectedResult  = "Return 403, message contains remaining minutes",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil = now + 15 min", "Status = Active", "Return 403" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_05 | B | LockedUntil expired → auto-unlock, login succeeds
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LockedUntilExpired_ShouldAllowLogin()
        {
            const string pass = "ValidPass123!";
            var user = ActiveUser(pass);
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(-1);

            var result = await CreateHandler(
                accountRepo:      BuildAccountRepoWith(user),
                configRepo:       BuildDefaultConfigMock(),
                gamification:     BuildGamificationMock(),
                emailHistoryRepo: BuildEmailHistoryMock())
                .Handle(new LoginCommand { Email = user.Email, Password = pass }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_05",
                Description     = "LockedUntil has expired (1 min ago) → auto-unlock, login succeeds",
                ExpectedResult  = "Return 200 login successful",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil = now - 1 min (expired)", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_06 | A | Wrong password → 400, FailedLoginCount increments
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ShouldReturn400AndIncrementFailedCount()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 0;

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            captured!.FailedLoginCount.Should().Be(1);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_06",
                Description     = "Wrong password → FailedLoginCount increments by 1",
                ExpectedResult  = "Return 400, FailedLoginCount = 1",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Password incorrect", "FailedLoginCount = 0 before", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_07 | B | Wrong password reaches limit (5th) → lock level 1
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesLimit_ShouldLockLevel1()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 4;

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            captured!.FailedLoginCount.Should().Be(5);
            captured.LockedUntil.Should().NotBeNull();
            captured.LockedUntil!.Value.Should().BeAfter(DateTime.UtcNow.AddHours(7));

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_07",
                Description     = "5th wrong password (= limit) → lock level 1 (5 min), LockedUntil set",
                ExpectedResult  = "FailedLoginCount = 5, LockedUntil is set, Return 400",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "FailedLoginCount = 4 before call",
                    "LOGIN_FAILED_LIMIT = 5",
                    "LOGIN_LOCKOUT_DURATION_LEVEL_1 = 300s",
                    "Return 400"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_08 | B | Wrong password reaches 2x limit → lock level 2 (30 min)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesDoubleLimit_ShouldLockLevel2()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 9;

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            captured!.FailedLoginCount.Should().Be(10);
            captured.LockedUntil!.Value.Should().BeAfter(DateTime.UtcNow.AddHours(7).AddMinutes(29));

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_08",
                Description     = "10th wrong password (limit x2) → lock level 2 (30 min)",
                ExpectedResult  = "LockedUntil = now + 30 min",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "FailedLoginCount = 9 before call",
                    "LOGIN_LOCKOUT_DURATION_LEVEL_2 = 1800s",
                    "LockedUntil > now + 29 min"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_09 | B | Wrong password ≥ 3x limit, PERMANENT_LOCK → Banned
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesTripleLimit_ShouldPermanentlyBan()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 14;

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            captured!.Status.Should().Be(AccountStatus.Banned);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_09",
                Description     = "15th wrong password (>= limit x3), action PERMANENT_LOCK → permanently banned",
                ExpectedResult  = "Status = Banned, LockedUntil = null",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "FailedLoginCount = 14 before call",
                    "LOGIN_LOCKOUT_LEVEL_3_ACTION = PERMANENT_LOCK",
                    "Status set to Banned"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_10 | A | Using default password → 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UsingDefaultPassword_ShouldReturn403()
        {
            const string defaultPass = "UserDefaultPass";
            var user = ActiveUser(defaultPass);

            var result = await CreateHandler(
                accountRepo: BuildAccountRepoWith(user),
                configRepo:  BuildDefaultConfigMock(defaultUser: defaultPass))
                .Handle(new LoginCommand { Email = user.Email, Password = defaultPass }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_10",
                Description     = "User logs in with the system default password → blocked",
                ExpectedResult  = "Return 403 DefaultPasswordUsed",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Password matches DEFAULT_PASSWORD_FOR_USER",
                    "BCrypt.Verify = true but then blocked",
                    "Return 403"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_11 | N | Valid credentials, RememberMe = false → 200, JWT, no refresh token
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCredentials_NoRememberMe_ShouldReturn200WithJwt()
        {
            const string pass = "ValidPass123!";
            var user = ActiveUser(pass);
            user.FailedLoginCount = 3;

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(
                accountRepo:      mockRepo,
                configRepo:       BuildDefaultConfigMock(),
                gamification:     BuildGamificationMock(),
                emailHistoryRepo: BuildEmailHistoryMock(),
                refreshTokenSvc:  BuildRefreshTokenMock())
                .Handle(new LoginCommand { Email = user.Email, Password = pass, RememberMe = false }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Token.Should().Be("fake-jwt-token");
            result.Data.RefreshToken.Should().BeNull();
            captured!.FailedLoginCount.Should().Be(0);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_11",
                Description     = "Valid credentials, RememberMe = false → JWT returned, no refresh token, FailedLoginCount reset",
                ExpectedResult  = "Return 200, token = fake-jwt-token, RefreshToken = null, FailedLoginCount = 0",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email + correct password",
                    "RememberMe = false → RefreshToken not created",
                    "FailedLoginCount reset to 0",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Login_12 | N | Valid credentials, RememberMe = true → 200, JWT + refresh token
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCredentials_RememberMe_ShouldReturn200WithRefreshToken()
        {
            const string pass = "ValidPass123!";
            var user = ActiveUser(pass);

            var result = await CreateHandler(
                accountRepo:      BuildAccountRepoWith(user),
                configRepo:       BuildDefaultConfigMock(),
                gamification:     BuildGamificationMock(),
                emailHistoryRepo: BuildEmailHistoryMock(),
                refreshTokenSvc:  BuildRefreshTokenMock())
                .Handle(new LoginCommand { Email = user.Email, Password = pass, RememberMe = true }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Token.Should().Be("fake-jwt-token");
            result.Data.RefreshToken.Should().Be("fake-refresh-token");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup   = "Login",
                TestCaseID      = "Login_12",
                Description     = "Valid credentials, RememberMe = true → both JWT and refresh token returned",
                ExpectedResult  = "Return 200, Token = fake-jwt-token, RefreshToken = fake-refresh-token",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email + correct password",
                    "RememberMe = true → RevokeAll + CreateRefreshToken called",
                    "Return 200 with both tokens"
                }
            });
        }
    }
}
