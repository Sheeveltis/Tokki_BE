using FluentAssertions;
using FluentValidation;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.Queries.Login;
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
     Mock<IAccountRepository>? accountRepo = null,
     Mock<ISystemConfigRepository>? configRepo = null,
     Mock<IJwtTokenGenerator>? jwtGen = null,
     Mock<IGamificationService>? gamification = null,
     Mock<IEmailHistoryRepository>? emailHistoryRepo = null)
        {
            var mockJwt = jwtGen ?? new Mock<IJwtTokenGenerator>();
            mockJwt.Setup(x => x.GenerateToken(It.IsAny<Account>(), It.IsAny<DateTime>()))
                   .Returns("fake-jwt-token");

            return new LoginCommandHandler(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (configRepo ?? BuildDefaultConfigMock()).Object,   // ← bỏ .Object bên trong
                mockJwt.Object,
                MockIdGeneratorService.GetMock().Object,
                (gamification ?? BuildGamificationMock()).Object,    // ← bỏ .Object bên trong
                new Mock<IValidator<LoginCommand>>().Object,
                (emailHistoryRepo ?? BuildEmailHistoryMock()).Object);   // ← bỏ .Object bên trong
        }
        // ── Sample data ──────────────────────────────────────────
        private static Account ActiveUser(string password = "ValidPass123!") => new()
        {
            UserId = "USER-001",
            Email = "user@tokki.com",
            FullName = "Test User",
            Role = AccountRole.User,
            Status = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FailedLoginCount = 0
        };

        // ── Shared mock builders ──────────────────────────────────
        private static Mock<ISystemConfigRepository> BuildDefaultConfigMock(
            string failedLimit = "5",
            string lockLevel1 = "300",
            string lockLevel2 = "1800",
            string lockLevel3 = "PERMANENT_LOCK",
            string tokenExp = "60",
            string defaultStaff = "StaffDefaultPass",
            string defaultUser = "UserDefaultPass",
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

        private static Mock<IAccountRepository> BuildAccountRepoWith(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-01  | A | Email không tồn tại → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

            var result = await CreateHandler(accountRepo: mockRepo)
                .Handle(new LoginCommand { Email = "ghost@tokki.com", Password = "Abc123" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-01",
                Description = "Email không tồn tại trong hệ thống",
                ExpectedResult = "Return 404 UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByEmailAsync trả về null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-02  | A | Tài khoản Inactive → 403
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
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-02",
                Description = "Tài khoản có status Inactive",
                ExpectedResult = "Return 403 AccountInActive",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccountStatus = Inactive" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-03  | A | Tài khoản Banned → 403
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
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-03",
                Description = "Tài khoản bị banned vĩnh viễn",
                ExpectedResult = "Return 403 AccountBanned",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccountStatus = Banned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-04  | A | Tài khoản đang bị tạm khóa → 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountLockedUntilFuture_ShouldReturn403WithRemainingMinutes()
        {
            var user = ActiveUser();
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(15); // còn 15 phút

            var result = await CreateHandler(accountRepo: BuildAccountRepoWith(user))
                .Handle(new LoginCommand { Email = user.Email, Password = "ValidPass123!" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("phút");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-04",
                Description = "Tài khoản đang bị tạm khóa, LockedUntil còn 15 phút",
                ExpectedResult = "Return 403, message chứa số phút còn lại",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil = now + 15 phút", "Status = Active" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-05  | B | LockedUntil đã hết hạn → được đăng nhập
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LockedUntilExpired_ShouldAllowLogin()
        {
            const string pass = "ValidPass123!";
            var user = ActiveUser(pass);
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(-1); // hết hạn 1 phút trước

            var result = await CreateHandler(
                accountRepo: BuildAccountRepoWith(user),
                configRepo: BuildDefaultConfigMock(),
                gamification: BuildGamificationMock(),
                emailHistoryRepo: BuildEmailHistoryMock())
                .Handle(new LoginCommand { Email = user.Email, Password = pass }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-05",
                Description = "LockedUntil đã qua → tài khoản được mở khóa tự động",
                ExpectedResult = "Return 200, đăng nhập thành công",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil = now - 1 phút (đã hết hạn)" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-06  | A | Sai mật khẩu → 400, FailedLoginCount tăng
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
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-06",
                Description = "Sai mật khẩu → FailedLoginCount tăng lên 1",
                ExpectedResult = "Return 400, FailedLoginCount = 1",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Password sai", "FailedLoginCount ban đầu = 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-07  | B | Sai mật khẩu đúng giới hạn (level 1) → tài khoản bị lock level 1
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesLimit_ShouldLockLevel1()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 4; // lần này sẽ là 5 = limit

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
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-07",
                Description = "Sai mật khẩu lần thứ 5 (= limit) → bị lock level 1 (5 phút)",
                ExpectedResult = "FailedLoginCount = 5, LockedUntil được set",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "FailedLoginCount = 4 trước khi gọi", "LOGIN_FAILED_LIMIT = 5" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-08  | B | Sai mật khẩu đúng giới hạn x2 → lock level 2
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesDoubleLimit_ShouldLockLevel2()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 9; // lần này = 10 = limit*2

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            captured!.FailedLoginCount.Should().Be(10);
            // Level 2 = 1800s = 30 phút → LockedUntil > now + 29 phút
            captured.LockedUntil!.Value.Should().BeAfter(DateTime.UtcNow.AddHours(7).AddMinutes(29));

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-08",
                Description = "Sai mật khẩu lần thứ 10 (= limit×2) → bị lock level 2 (30 phút)",
                ExpectedResult = "LockedUntil = now + 30 phút",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "FailedLoginCount = 9 trước khi gọi", "LOGIN_FAILED_LIMIT = 5" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-09  | B | Sai mật khẩu ≥ limit×3, action=PERMANENT_LOCK → Banned
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongPassword_ReachesTripleLimit_ShouldPermanentlyBan()
        {
            var user = ActiveUser("CorrectPass123!");
            user.FailedLoginCount = 14; // lần này = 15 = limit*3

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(accountRepo: mockRepo, configRepo: BuildDefaultConfigMock())
                .Handle(new LoginCommand { Email = user.Email, Password = "WrongPass" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            captured!.Status.Should().Be(AccountStatus.Banned);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-09",
                Description = "Sai mật khẩu lần thứ 15 (≥ limit×3), action=PERMANENT_LOCK → ban vĩnh viễn",
                ExpectedResult = "Status = Banned, LockedUntil = null",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "FailedLoginCount = 14 trước khi gọi",
                    "LOGIN_LOCKOUT_LEVEL_3_ACTION = PERMANENT_LOCK"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-10  | A | Dùng default password → 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UsingDefaultPassword_ShouldReturn403()
        {
            const string defaultPass = "UserDefaultPass";
            var user = ActiveUser(defaultPass);

            var result = await CreateHandler(
                accountRepo: BuildAccountRepoWith(user),
                configRepo: BuildDefaultConfigMock(defaultUser: defaultPass))
                .Handle(new LoginCommand { Email = user.Email, Password = defaultPass }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-10",
                Description = "Người dùng đăng nhập bằng đúng default password",
                ExpectedResult = "Return 403 DefaultPasswordUsed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Password khớp với DEFAULT_PASSWORD_FOR_USER",
                    "BCrypt.Verify = true nhưng bị chặn sau đó"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-LOGIN-11  | N | Đăng nhập thành công → 200, token, reset FailedCount
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCredentials_ShouldReturn200AndResetFailedCount()
        {
            const string pass = "ValidPass123!";
            var user = ActiveUser(pass);
            user.FailedLoginCount = 3; // có lỗi trước đó, phải reset

            Account? captured = null;
            var mockRepo = BuildAccountRepoWith(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(u => captured = u)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(
                accountRepo: mockRepo,
                configRepo: BuildDefaultConfigMock(),
                gamification: BuildGamificationMock(),
                emailHistoryRepo: BuildEmailHistoryMock())
                .Handle(new LoginCommand { Email = user.Email, Password = pass }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Token.Should().Be("fake-jwt-token");
            captured!.FailedLoginCount.Should().Be(0);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-11",
                Description = "Đăng nhập thành công → trả JWT, reset FailedLoginCount về 0",
                ExpectedResult = "Return 200, Token hợp lệ, FailedLoginCount = 0",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email + password",
                    "FailedLoginCount = 3 trước đó → reset về 0",
                    "Không phải default password"
                }
            });
        }

       
    }
}