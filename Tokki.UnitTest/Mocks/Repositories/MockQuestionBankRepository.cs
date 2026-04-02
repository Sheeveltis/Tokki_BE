using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockQuestionBankRepository
    {
        public static Mock<IQuestionBankRepository> GetMock(
            QuestionBank? returnedQuestion = null,
            List<QuestionBank>? returnedByIds = null)
        {
            var mockRepo = new Mock<IQuestionBankRepository>();

            mockRepo.Setup(x => x.GetByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedQuestion);

            mockRepo.Setup(x => x.GetByIdsWithDetailsAsync(
                        It.IsAny<List<string>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByIds ?? new List<QuestionBank>());

            mockRepo.Setup(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
            return mockRepo;
        }

        public static QuestionBank GetSamplePendingQB(string id = "QB-001")
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = QuestionBankStatus.PendingApproval,
                CreateBy = "STAFF-001",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleActiveQB(string id = "QB-002")
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = QuestionBankStatus.Active,
                CreateBy = "STAFF-001",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleDeletedQB(string id = "QB-003")
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = QuestionBankStatus.Deleted,
                CreateBy = "STAFF-001",
                QuestionOptions = new List<QuestionOption>()
            };
        }
    }
}