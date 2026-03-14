using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockUserVocabProgressRepository
    {
        public static Mock<IUserVocabProgressRepository> GetMock(
            UserVocabProgress? existingProgress = null,
            List<ReviewItemDTO>? dueReviews = null)
        {
            var mockRepo = new Mock<IUserVocabProgressRepository>();

            // Setup GetByVocabIdAsync
            mockRepo.Setup(x => x.GetByVocabIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingProgress);

            // Setup AddAsync
            mockRepo.Setup(x => x.AddAsync(
                        It.IsAny<UserVocabProgress>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

            // Setup GetDueReviewsAsync
            mockRepo.Setup(x => x.GetDueReviewsAsync(
                        It.IsAny<string>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dueReviews ?? new List<ReviewItemDTO>());

            return mockRepo;
        }

        // Data mẫu: progress đang ở Learning (mới bắt đầu)
        public static UserVocabProgress GetLearningProgress(string userId, string vocabId)
        {
            return new UserVocabProgress
            {
                UserVocabProgressId = "PROG-FAKE-001",
                UserId = userId,
                VocabularyId = vocabId,
                BoxLevel = BoxLevel.Learning,
                Streak = 0,
                IsMastered = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Data mẫu: progress đang ở Mastered, Streak = 1 (boundary: 1 correct → mastered)
        public static UserVocabProgress GetMasteredStreakOneProgress(string userId, string vocabId)
        {
            return new UserVocabProgress
            {
                UserVocabProgressId = "PROG-FAKE-002",
                UserId = userId,
                VocabularyId = vocabId,
                BoxLevel = BoxLevel.Mastered,
                Streak = 1,
                IsMastered = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Data mẫu: danh sách due reviews
        public static List<ReviewItemDTO> GetSampleDueReviews()
        {
            return new List<ReviewItemDTO>
            {
                new ReviewItemDTO { VocabularyId = "VOCAB-001" },
                new ReviewItemDTO { VocabularyId = "VOCAB-002" }
            };
        }
    }
}