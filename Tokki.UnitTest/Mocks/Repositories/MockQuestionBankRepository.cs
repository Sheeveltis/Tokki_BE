using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockQuestionBankRepository
    {
        /// <summary>
        /// Creates a fully configured Mock of IQuestionBankRepository.
        /// </summary>
        public static Mock<IQuestionBankRepository> GetMock(
            QuestionBank?              returnedById         = null,
            QuestionBank?              returnedByIdWithDetails = null,
            List<QuestionBank>?        returnedByIds        = null,
            List<QuestionBank>?        returnedByIdsWithDetails = null,
            IEnumerable<QuestionBank>? returnedByTypeId     = null,
            (IEnumerable<QuestionBank> items, int totalCount)? pagedResult = null)
        {
            var mockRepo = new Mock<IQuestionBankRepository>();

            // GetByIdAsync
            mockRepo.Setup(x => x.GetByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedById);

            // GetByIdWithDetailsAsync
            mockRepo.Setup(x => x.GetByIdWithDetailsAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByIdWithDetails);

            // GetByIdsAsync
            mockRepo.Setup(x => x.GetByIdsAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByIds ?? new List<QuestionBank>());

            // GetByIdsWithDetailsAsync
            mockRepo.Setup(x => x.GetByIdsWithDetailsAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByIdsWithDetails ?? new List<QuestionBank>());

            // GetByQuestionTypeIdAsync (no status)
            mockRepo.Setup(x => x.GetByQuestionTypeIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByTypeId ?? new List<QuestionBank>());

            // GetByQuestionTypeIdAsync (with status)
            mockRepo.Setup(x => x.GetByQuestionTypeIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<QuestionBankStatus?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedByTypeId ?? new List<QuestionBank>());

            // GetPagedAsync
            var paged = pagedResult ?? (new List<QuestionBank>(), 0);
            mockRepo.Setup(x => x.GetPagedAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<QuestionBankStatus?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(paged);

            // Mutations
            mockRepo.Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<QuestionBank>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            // GetExistingContentsAsync
            mockRepo.Setup(x => x.GetExistingContentsAsync(It.IsAny<List<string>>()))
                    .ReturnsAsync(new List<string>());

            // GetQuestionsByTypeAsync
            mockRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                    .ReturnsAsync(new List<QuestionSignatureDTO>());

            return mockRepo;
        }

        // ===== Sample Data =====

        public static QuestionBank GetSamplePendingQB(string id = "QB-001", string questionTypeId = "QT-001")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.PendingApproval,
                CreateBy        = "STAFF-001",
                QuestionTypeId  = questionTypeId,
                Content         = "Sample question content",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleDraftQB(string id = "QB-DRAFT-01", string questionTypeId = "QT-001")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.Draft,
                CreateBy        = "STAFF-001",
                QuestionTypeId  = questionTypeId,
                Content         = "Draft question content",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleActiveQB(string id = "QB-002", string questionTypeId = "QT-001")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.Active,
                CreateBy        = "STAFF-001",
                QuestionTypeId  = questionTypeId,
                Content         = "Active question content",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleRejectedQB(string id = "QB-REJ-01", string questionTypeId = "QT-001")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.Rejected,
                CreateBy        = "STAFF-001",
                QuestionTypeId  = questionTypeId,
                Content         = "Rejected question content",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleDeletedQB(string id = "QB-003")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.Deleted,
                CreateBy        = "STAFF-001",
                QuestionOptions = new List<QuestionOption>()
            };
        }

        public static QuestionBank GetSampleQBWithOptions(string id = "QB-OPT-01")
        {
            return new QuestionBank
            {
                QuestionBankId  = id,
                Status          = QuestionBankStatus.Active,
                QuestionTypeId  = "QT-001",
                Content         = "Which sentence is correct?",
                QuestionOptions = new List<QuestionOption>
                {
                    new QuestionOption { OptionId = "OPT-A", KeyOption = "A", Content = "Option A", IsCorrect = true },
                    new QuestionOption { OptionId = "OPT-B", KeyOption = "B", Content = "Option B", IsCorrect = false }
                }
            };
        }

        public static List<QuestionBank> GetSampleQBList()
        {
            return new List<QuestionBank>
            {
                GetSampleActiveQB("QB-LIST-01"),
                GetSampleActiveQB("QB-LIST-02"),
                GetSampleActiveQB("QB-LIST-03")
            };
        }
    }
}