using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Register;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class RegisterUserAccountCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static RegisterUserAccountCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IIdGeneratorService>? idGen = null)
            => new(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (idGen ?? MockIdGeneratorService.GetMock()).Object);

        private static RegisterUserAccountCommand BuildCommand(
            string email    = "newuser@tokki.com",
            string password = "ValidPass123!",
            string fullName = "New User",
            string? phone   = "0912345678")
            => new()
            {
                Email       = email,
                Password    = password,
                FullName    = fullName,
                PhoneNumber = phone,
                DateOfBirth = new DateOnly(2000, 1, 1)
            };

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_01 | A | Duplicate email → 409
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateEmail_ShouldReturn409()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync("dup@tokki.com")).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(BuildCommand(email: "dup@tokki.com"), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_01",
                Description       = "Email already registered in the system",
                ExpectedResult    = "Return 409 EmailDuplicated",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsEmailExistsAsync = true", "Return 409" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_02 | A | Duplicate phone number → 409
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicatePhone_ShouldReturn409()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.IsPhoneNumberExistsAsync("0911111111")).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(BuildCommand(phone: "0911111111"), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_02",
                Description       = "Phone number already exists in the system",
                ExpectedResult    = "Return 409 PhoneNumberDuplicated",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsPhoneNumberExistsAsync = true", "Return 409" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_03 | N | Valid registration without phone → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRegistration_NoPhone_ShouldReturn201()
        {
            var result = await CreateHandler().Handle(BuildCommand(phone: null), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_03",
                Description       = "Valid registration without phone number (optional field skipped)",
                ExpectedResult    = "Return 201, UserId in Data, phone check skipped",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email not duplicate",
                    "PhoneNumber = null → phone check skipped",
                    "Account created with Role = User, Status = Active",
                    "Return 201"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_04 | N | Valid registration with phone → account has correct role/status/hash
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRegistration_WithPhone_ShouldCreateCorrectAccount()
        {
            Account? captured = null;
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(BuildCommand(phone: "0912345678"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            captured!.Role.Should().Be(AccountRole.User);
            captured.Status.Should().Be(AccountStatus.Active);
            BCrypt.Net.BCrypt.Verify("ValidPass123!", captured.PasswordHash).Should().BeTrue();

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_04",
                Description       = "Valid registration with phone → Role = User, Status = Active, password hashed",
                ExpectedResult    = "Return 201, Role = User, Status = Active, BCrypt hash correct",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email not duplicate, Phone not duplicate",
                    "Password correctly BCrypt hashed",
                    "Role = User, Status = Active",
                    "Return 201"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_05 | N | IsPhoneNumberExistsAsync NOT called when phone is null
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoPhone_ShouldNotCallPhoneCheck()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            await CreateHandler(mockRepo).Handle(BuildCommand(phone: null), CancellationToken.None);

            mockRepo.Verify(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_05",
                Description       = "No phone provided → IsPhoneNumberExistsAsync is never called",
                ExpectedResult    = "IsPhoneNumberExistsAsync called × 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "PhoneNumber = null",
                    "IsPhoneNumberExistsAsync NOT called",
                    "Return 201"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Register_User_Account_06 | N | AddAsync and SaveChangesAsync called on success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRegistration_ShouldCallAddAndSave()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            await CreateHandler(mockRepo).Handle(BuildCommand(), CancellationToken.None);

            mockRepo.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Register", new TestCaseDetail
            {
                FunctionGroup     = "Register User Account",
                TestCaseID        = "Register_User_Account_06",
                Description       = "Valid registration → AddAsync and SaveChangesAsync each called once",
                ExpectedResult    = "AddAsync × 1, SaveChangesAsync × 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email not duplicate, phone not duplicate",
                    "AddAsync called × 1",
                    "SaveChangesAsync called × 1"
                }
            });
        }
    }
}
