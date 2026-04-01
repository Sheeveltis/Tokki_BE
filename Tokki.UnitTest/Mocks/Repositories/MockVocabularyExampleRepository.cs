using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockVocabularyExampleRepository
    {
        public static Mock<IVocabularyExampleRepository> GetMock(
            VocabularyExample? existingExample = null,
            VocabularyExample? existingById = null,
            List<VocabularyExample>? returnedByVocabId = null)
        {
            var mockRepo = new Mock<IVocabularyExampleRepository>();

            // BeginTransactionAsync — fake transaction
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
            mockTransaction.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
            mockTransaction.Setup(x => x.DisposeAsync())
                           .Returns(ValueTask.CompletedTask);

            mockRepo.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockTransaction.Object);

            // GetBySentenceAsync — dùng để check duplicate
            mockRepo.Setup(x => x.GetBySentenceAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(existingExample);

            // GetByIdAsync — dùng cho Delete/Update
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(existingById);

            // GetByVocabularyIdAsync — dùng cho query
            mockRepo.Setup(x => x.GetByVocabularyIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedByVocabId ?? new List<VocabularyExample>());

            // AddAsync
            mockRepo.Setup(x => x.AddAsync(It.IsAny<VocabularyExample>()))
                    .Returns(Task.CompletedTask);

            // UpdateAsync
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<VocabularyExample>()))
                    .Returns(Task.CompletedTask);

            // SaveChangesAsync
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(1);
            return mockRepo;
        }

        // ===== Sample Data =====

        public static VocabularyExample GetSampleExample(
            string exampleId = "EX-001",
            string vocabId = "VOCAB-001",
            VocabularyExampleStatus status = VocabularyExampleStatus.Active)
        {
            return new VocabularyExample
            {
                ExampleId = exampleId,
                VocabularyId = vocabId,
                Sentence = "안녕하세요, 만나서 반갑습니다.",
                Translation = "Hi, nice to meet you.",
                CreateBy = "USER-001",
                Status = status
            };
        }

        public static VocabularyExample GetSampleDeletedExample(string exampleId = "EX-002")
        {
            return new VocabularyExample
            {
                ExampleId = exampleId,
                VocabularyId = "VOCAB-001",
                Sentence = "Sentence deleted.",
                Status = VocabularyExampleStatus.Deleted
            };
        }

        public static List<VocabularyExample> GetSampleExampleList(string vocabId = "VOCAB-001")
        {
            return new List<VocabularyExample>
            {
                new VocabularyExample
                {
                    ExampleId = "EX-001",
                    VocabularyId = vocabId,
                    Sentence = "안녕하세요.",
                    Translation = "Hello.",
                    Status = VocabularyExampleStatus.Active
                },
                new VocabularyExample
                {
                    ExampleId = "EX-002",
                    VocabularyId = vocabId,
                    Sentence = "감사합니다.",
                    Translation = "Thank.",
                    Status = VocabularyExampleStatus.Active
                }
            };
        }
    }
}