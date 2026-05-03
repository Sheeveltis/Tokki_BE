using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.Commands.SubmitReview;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabSpacedRepetition
{
    public class SubmitReviewCommandHandlerTests
    {
        private SubmitReviewCommandHandler CreateHandler(
            Mock<IUserVocabProgressRepository>? progressRepo = null,
            Mock<IVocabularyRepository>? vocabRepo = null)
        {
            return new SubmitReviewCommandHandler(
                (progressRepo ?? MockUserVocabProgressRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_VocabularyNotFound_ShouldReturn400()
        {
            // Arrange
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-INVALID",
                IsCorrect = true
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be(AppErrors.VocabularyNotFound.Description);

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_01",
                Description = "Submit review with VocabularyId does not exist in the system",
                ExpectedResult = "Return 400 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VocabularyId",
                    "Vocabulary = null",
                    "Return 400 Bad Request"
                }
            });
        }

        [Fact]
        public async Task Handle_NoExistingProgress_CorrectAnswer_ShouldCreateProgressAndAdvanceBoxLevel()
        {
            // Arrange — không có progress → tạo mới, trả lời đúng → BoxLevel Learning → Reviewing
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                IsCorrect = true
            };

            var mockProgressRepo = MockUserVocabProgressRepository.GetMock(
                existingProgress: null); // null → tạo mới

            var handler = CreateHandler(
                progressRepo: mockProgressRepo,
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.VocabularyId.Should().Be(command.VocabularyId);
            result.Data.IsMastered.Should().BeFalse();

            // Verify AddAsync được gọi 1 lần (progress mới được tạo)
            mockProgressRepo.Verify(
                x => x.AddAsync(It.IsAny<UserVocabProgress>(), It.IsAny<CancellationToken>()),
                Times.Once);

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_02",
                Description = "First time reviewing vocab: no progress yet, correct answer → create new progress and increase BoxLevel",
                ExpectedResult = "New progress created, IsMastered = false, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No existing UserVocabProgress",
                    "Valid VocabularyId",
                    "IsCorrect = true",
                    "AddAsync called once",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_AtMasteredLevel_Streak1_CorrectAnswer_ShouldSetIsMasteredTrue()
        {
            // Arrange — Streak = 1, BoxLevel = Mastered, trả lời đúng → Streak = 2 → IsMastered = true
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                IsCorrect = true
            };

            var existingProgress = MockUserVocabProgressRepository
                .GetMasteredStreakOneProgress("USER-001", "VOCAB-001");

            var handler = CreateHandler(
                progressRepo: MockUserVocabProgressRepository.GetMock(
                    existingProgress: existingProgress),
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.IsMastered.Should().BeTrue();
            existingProgress.Streak.Should().Be(2);
            existingProgress.IntervalDays.Should().Be(90);

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_03",
                Description = "BoxLevel = Mastered, Streak = 1, correct answer → Streak reaches 2 → IsMastered = true, IntervalDays = 90",
                ExpectedResult = "IsMastered = true, Streak = 2, IntervalDays = 90, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BoxLevel = Mastered",
                    "Streak = 1 (boundary: 1 step before mastered)",
                    "IsCorrect = true",
                    "Streak >= 2 → IsMastered = true"
                }
            });
        }

        [Fact]
        public async Task Handle_AtLearningLevel_WrongAnswer_ShouldStayAtLearningAndResetStreak()
        {
            // Arrange — BoxLevel = Learning (minimum), trả lời sai → giữ nguyên Learning, Streak reset = 0
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                IsCorrect = false
            };

            var existingProgress = MockUserVocabProgressRepository
                .GetLearningProgress("USER-001", "VOCAB-001");
            existingProgress.Streak = 3; // có streak trước đó

            var handler = CreateHandler(
                progressRepo: MockUserVocabProgressRepository.GetMock(
                    existingProgress: existingProgress),
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingProgress.BoxLevel.Should().Be(BoxLevel.Learning); // không thể giảm hơn nữa
            existingProgress.Streak.Should().Be(0);                   // streak bị reset
            existingProgress.IsMastered.Should().BeFalse();

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_04",
                Description = "BoxLevel = Learning (minimum), wrong answer → BoxLevel remains Learning, Streak reset = 0",
                ExpectedResult = "BoxLevel = Learning, Streak = 0, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BoxLevel = Learning (boundary: minimum level)",
                    "IsCorrect = false",
                    "BoxLevel cannot be reduced further",
                    "Streak resets to 0"
                }
            });
        }

        [Fact]
        public async Task Handle_IsMasteredTrue_WrongAnswer_ShouldRevokeMastered()
        {
            // Arrange — đang IsMastered = true, trả lời sai → IsMastered = false
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                IsCorrect = false
            };

            var masteredProgress = new UserVocabProgress
            {
                UserVocabProgressId = "PROG-001",
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                BoxLevel = BoxLevel.Mastered,
                Streak = 3,
                IsMastered = true, // đang mastered
                CreatedAt = DateTime.UtcNow
            };

            var handler = CreateHandler(
                progressRepo: MockUserVocabProgressRepository.GetMock(
                    existingProgress: masteredProgress),
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.IsMastered.Should().BeFalse(); // IsMastered bị thu hồi
            masteredProgress.Streak.Should().Be(0);
            masteredProgress.BoxLevel.Should().Be(BoxLevel.Advanced); // giảm 1 bậc

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_05",
                Description = "IsMastered = true, wrong answer → IsMastered = false, BoxLevel decreases by 1 level",
                ExpectedResult = "IsMastered = false, BoxLevel = Advanced, Streak = 0, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsMastered = true",
                    "IsCorrect = false",
                    "IsMastered revoked → false",
                    "BoxLevel decreases from Mastered → Advanced"
                }
            });
        }

        [Fact]
        public async Task Handle_CorrectAnswer_IntervalDays_ShouldMatchBoxLevel()
        {
            // Arrange — BoxLevel = Reviewing, trả lời đúng → BoxLevel tăng lên Mastering → IntervalDays = 7
            var command = new SubmitReviewCommand
            {
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                IsCorrect = true
            };

            var existingProgress = new UserVocabProgress
            {
                UserVocabProgressId = "PROG-001",
                UserId = "USER-001",
                VocabularyId = "VOCAB-001",
                BoxLevel = BoxLevel.Reviewing,
                Streak = 0,
                IsMastered = false,
                CreatedAt = DateTime.UtcNow
            };

            var handler = CreateHandler(
                progressRepo: MockUserVocabProgressRepository.GetMock(
                    existingProgress: existingProgress),
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingProgress.BoxLevel.Should().Be(BoxLevel.Mastering);
            existingProgress.IntervalDays.Should().Be(7); // Mastering = 7 ngày
            existingProgress.NextReviewAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));

            QACollector.LogTestCase("VocabSR - Submit Review", new TestCaseDetail
            {
                FunctionGroup = "Submit Review",
                TestCaseID = "Submit_Review_06",
                Description = "BoxLevel = Reviewing, correct answer → BoxLevel increases to Mastering → IntervalDays = 7",
                ExpectedResult = "BoxLevel = Mastering, IntervalDays = 7, NextReviewAt ≈ now + 7 days",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BoxLevel = Reviewing",
                    "IsCorrect = true",
                    "BoxLevel increased to Mastering",
                    "IntervalDays = 7 theo GetIntervalByLevel"
                }
            });
        }
    }
}