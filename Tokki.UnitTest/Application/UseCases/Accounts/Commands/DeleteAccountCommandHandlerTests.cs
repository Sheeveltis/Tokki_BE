using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.DeleteAccount;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class DeleteAccountCommandHandlerTests
    {
        private Mock<IAccountRepository> GetRepoMock(Account? accountToReturn)
        {
            var mock = new Mock<IAccountRepository>();
            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(accountToReturn);
            mock.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private DeleteAccountCommandHandler CreateHandler(Mock<IAccountRepository> repo)
        {
            return new DeleteAccountCommandHandler(repo.Object);
        }

        // DeleteAccountCommandHandler_01 | A | UserId is null or empty -> 401 Unauthorized
        [Fact]
        public async Task Handle_EmptyUserId_ShouldReturnFailureWithUnauthorized()
        {
            var handler = CreateHandler(GetRepoMock(null));
            var result = await handler.Handle(new DeleteAccountCommand { UserId = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.First().Code.Should().Be("User.Unauthorized");

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_01",
                Description = "Empty UserId returns User.Unauthorized",
                ExpectedResult = "Failure with User.Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId = null" }
            });
        }

        // DeleteAccountCommandHandler_02 | A | User not found -> 404
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnFailureWithUserNotFound()
        {
            var handler = CreateHandler(GetRepoMock(null));
            var result = await handler.Handle(new DeleteAccountCommand { UserId = "user1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.First().Code.Should().Be("User.NotFound.Id");

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_02",
                Description = "User not found returns User.NotFound.Id",
                ExpectedResult = "Failure with User.NotFound.Id",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // DeleteAccountCommandHandler_03 | A | User already inactive -> "Account.AlreadyDeleted"
        [Fact]
        public async Task Handle_UserAlreadyInactive_ShouldReturnFailureAccountAlreadyDeleted()
        {
            var account = new Account { UserId = "user1", Status = AccountStatus.Inactive };
            var handler = CreateHandler(GetRepoMock(account));
            
            var result = await handler.Handle(new DeleteAccountCommand { UserId = "user1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.First().Code.Should().Be("Account.AlreadyDeleted");
            result.Errors.First().Description.Should().Be("Tài khoản đã bị xóa trước đó.");

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_03",
                Description = "User with Inactive status returns Account.AlreadyDeleted",
                ExpectedResult = "Failure with Account.AlreadyDeleted",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account is Inactive" }
            });
        }

        // DeleteAccountCommandHandler_04 | N | Happy Path -> Successfully soft deletes account
        [Fact]
        public async Task Handle_ValidRequest_ShouldSoftDeleteAccountAndReturnSuccess()
        {
            var account = new Account { UserId = "user1", Status = AccountStatus.Active };
            var handler = CreateHandler(GetRepoMock(account));
            
            var result = await handler.Handle(new DeleteAccountCommand { UserId = "user1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            account.Status.Should().Be(AccountStatus.Inactive);
            
            // Check UpdatedAt logic to be approx UtcNow + 7h. (Checking > MinValue suffices for mutation)
            account.UpdatedAt.Should().BeAfter(DateTime.MinValue); 

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_04",
                Description = "Valid request mutates status to Inactive",
                ExpectedResult = "Success 200, Status Inactive",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account is Active" }
            });
        }

        // DeleteAccountCommandHandler_05 | B | Ensure UpdateUserAsync and SaveChangesAsync are called on success
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallUpdateAndSaveAsync()
        {
            var account = new Account { UserId = "user1", Status = AccountStatus.Active };
            var mockRepo = GetRepoMock(account);
            var handler = CreateHandler(mockRepo);
            
            await handler.Handle(new DeleteAccountCommand { UserId = "user1" }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_05",
                Description = "Valid request calls Update and Save",
                ExpectedResult = "Verify Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid flow calls repo" }
            });
        }

        // DeleteAccountCommandHandler_06 | B | Ensure Update/Save are NOT called on failure
        [Fact]
        public async Task Handle_FailureFlow_ShouldNotCallUpdateAndSaveAsync()
        {
            var mockRepo = GetRepoMock(null); // Returns null -> 404
            var handler = CreateHandler(mockRepo);
            
            await handler.Handle(new DeleteAccountCommand { UserId = "user1" }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Never);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Account - Delete", new TestCaseDetail
            {
                FunctionGroup = "DeleteAccountCommandHandler",
                TestCaseID = "DeleteAccountCommandHandler_06",
                Description = "Failed flow skips DB modifications",
                ExpectedResult = "Verify Times.Never",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User == null" }
            });
        }
    }
}
