using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockAccountRepository
    {
        public static Mock<IAccountRepository> GetMock(
            List<Account>? existingAccounts = null,
            List<string>? existingEmails = null)
        {
            var mockRepo = new Mock<IAccountRepository>();

            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingAccounts ?? new List<Account>());

            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingEmails ?? new List<string>());

            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            return mockRepo;
        }

        // Data mẫu xịn xò để test Export
        public static List<Account> GetSampleAccountsForExport()
        {
            return new List<Account>
            {
                new Account { FullName = "Nguyễn Văn A", Email = "nva@tokki.com", Role = AccountRole.User, PhoneNumber = "0901234567" },
                new Account { FullName = "Trần Thị B", Email = "ttb@tokki.com", Role = AccountRole.Admin, PhoneNumber = "0987654321" }
            };
        }
    }
}