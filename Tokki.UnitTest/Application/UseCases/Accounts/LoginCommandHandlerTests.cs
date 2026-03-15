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
        private LoginCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<ISystemConfigRepository>? configRepo = null,
            Mock<IJwtTokenGenerator>? jwtGen = null,
            Mock<IGamificationService>? gamification = null,
            Mock<IEmailHistoryRepository>? emailHistoryRepo = null)
        {
            var mockJwt = jwtGen ?? new Mock<IJwtTokenGenerator>();
            mockJwt.Setup(x => x.GenerateToken(
                        It.IsAny<Account>(),
                        It.IsAny<DateTime>()))
                   .Returns("fake-jwt-token");

            return new LoginCommandHandler(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (configRepo ?? MockSystemConfigRepository.GetMock()).Object,
                mockJwt.Object,
                MockIdGeneratorService.GetMock().Object,
                (gamification ?? new Mock<IGamificationService>()).Object,
                new Mock<IValidator<LoginCommand>>().Object,
                (emailHistoryRepo ?? new Mock<IEmailHistoryRepository>()).Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var command = new LoginCommand
            {
                Email = "notfound@tokki.com",
                Password = "Password123"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((Account?)null);

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-01",
                Description = "Đăng nhập với email không tồn tại",
                ExpectedResult = "Return 404 UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email không tồn tại",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_AccountBanned_ShouldReturn403()
        {
            var command = new LoginCommand
            {
                Email = "banned@tokki.com",
                Password = "Password123"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "banned@tokki.com",
                               Status = AccountStatus.Banned,
                               PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
                           });

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-02",
                Description = "Đăng nhập với tài khoản bị banned",
                ExpectedResult = "Return 403 AccountBanned",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AccountStatus = Banned",
                    "Return 403"
                }
            });
        }

        [Fact]
        public async Task Handle_AccountLocked_ShouldReturn403()
        {
            var command = new LoginCommand
            {
                Email = "locked@tokki.com",
                Password = "Password123"
            };

            var lockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(10);

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "locked@tokki.com",
                               Status = AccountStatus.Active,
                               LockedUntil = lockedUntil,
                               PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
                           });

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-03",
                Description = "Đăng nhập với tài khoản đang bị tạm khóa",
                ExpectedResult = "Return 403 AccountLocked",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "LockedUntil > now",
                    "Return 403 với remaining minutes"
                }
            });
        }

        [Fact]
        public async Task Handle_WrongPassword_ShouldReturn400()
        {
            var command = new LoginCommand
            {
                Email = "user@tokki.com",
                Password = "WrongPassword"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "user@tokki.com",
                               Status = AccountStatus.Active,
                               PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                               FailedLoginCount = 0
                           });

            mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                           .Returns(Task.CompletedTask);

            mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

            var mockConfig = MockSystemConfigRepository.GetMock("5"); // LOGIN_FAILED_LIMIT = 5

            var handler = CreateHandler(
                accountRepo: mockAccountRepo,
                configRepo: mockConfig);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-04",
                Description = "Đăng nhập với mật khẩu sai",
                ExpectedResult = "Return 400 WrongPassword",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Password không match BCrypt hash",
                    "FailedLoginCount tăng lên",
                    "Return 400"
                }
            });
        }

        // ⚠️ NOTE: Test này có thể FAIL vì handler gọi BCrypt.Verify + IsUsingAnyDefaultPasswordAsync
        // cần mock SystemConfig trả về đúng giá trị
        [Fact]
        public async Task Handle_ValidCredentials_ShouldReturn200WithToken()
        {
            const string rawPassword = "ValidPassword123!";

            var command = new LoginCommand
            {
                Email = "user@tokki.com",
                Password = rawPassword
            };

            var user = new Account
            {
                UserId = "USER-001",
                Email = "user@tokki.com",
                Status = AccountStatus.Active,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                FailedLoginCount = 0,
                Role = AccountRole.User,
                FullName = "Test User"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(user);
            mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                           .Returns(Task.CompletedTask);
            mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
            mockAccountRepo.Setup(x => x.AddSessionAsync(It.IsAny<Session>()))
                           .Returns(Task.CompletedTask);

            // Config trả về password khác với password của user → pass check default password
            var mockConfig = new Mock<ISystemConfigRepository>();
            mockConfig.Setup(x => x.GetValueByKeyAsync("TOKEN_EXPIRATION_MINUTES"))
                      .ReturnsAsync("60");
            mockConfig.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT"))
                      .ReturnsAsync("5");
            mockConfig.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                      .ReturnsAsync("StaffDefaultPass");
            mockConfig.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER"))
                      .ReturnsAsync("UserDefaultPass");
            mockConfig.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN"))
                      .ReturnsAsync("AdminDefaultPass");

            var mockGamification = new Mock<IGamificationService>();
            mockGamification.Setup(x => x.CheckLoginGamificationAsync(It.IsAny<Account>()))
                            .Returns(Task.CompletedTask);

            var mockEmailHistory = new Mock<IEmailHistoryRepository>();
            mockEmailHistory.Setup(x => x.DeleteByUserAndTemplateTypeAsync(
                        It.IsAny<string>(),
                        It.IsAny<EmailTemplateType>(),
                        It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                accountRepo: mockAccountRepo,
                configRepo: mockConfig,
                gamification: mockGamification,
                emailHistoryRepo: mockEmailHistory);

            var result = await handler.Handle(command, CancellationToken.None);

            // ⚠️ Có thể fail nếu mock config không đúng
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Token.Should().Be("fake-jwt-token");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "Login",
                TestCaseID = "TC-LOGIN-05",
                Description = "Đăng nhập hợp lệ → trả về JWT token",
                ExpectedResult = "Return 200, Token = 'fake-jwt-token'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email và password",
                    "Account Active",
                    "Password không phải default",
                    "Return 200 với JWT"
                }
            });
        }
    }
}