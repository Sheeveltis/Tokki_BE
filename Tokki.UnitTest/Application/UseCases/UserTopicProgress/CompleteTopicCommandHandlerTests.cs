using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserTopicProgress
{
    public class CompleteTopicCommandHandlerTests
    {
        private CompleteTopicCommandHandler CreateHandler(
            Mock<IUserTopicProgressRepository>? progressRepo = null,
            Mock<ITopicRepository>? topicRepo = null)
        {
            return new CompleteTopicCommandHandler(
                (progressRepo ?? new Mock<IUserTopicProgressRepository>()).Object,
                (topicRepo ?? new Mock<ITopicRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        // ── TC-01: Topic không tồn tại → 404 ─────────────────────────────
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-INVALID", UserId = "USER-001" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-INVALID"))
                         .ReturnsAsync((Topic?)null);

            var handler = CreateHandler(topicRepo: mockTopicRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_01",
                Description = "TopicId does not exist → return 404",
                ExpectedResult = "IsSuccess=false, StatusCode=404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ── TC-02: Không có progress trước đó → AddAsync được gọi ────────
        [Fact]
        public async Task Handle_NoExistingProgress_ShouldAddNewRecord()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-001", UserId = "USER-001" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-001"))
                         .ReturnsAsync(new Topic { TopicId = "TOPIC-001" });

            var mockProgressRepo = new Mock<IUserTopicProgressRepository>();
            mockProgressRepo.Setup(x => x.GetByUserIdAndTopicIdAsync("USER-001", "TOPIC-001"))
                            .ReturnsAsync((Domain.Entities.UserTopicProgress?)null);
            mockProgressRepo.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()))
                            .Returns(Task.CompletedTask);
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = CreateHandler(progressRepo: mockProgressRepo, topicRepo: mockTopicRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            mockProgressRepo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()), Times.Once);
            mockProgressRepo.Verify(x => x.Update(It.IsAny<Domain.Entities.UserTopicProgress>()), Times.Never);

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_02",
                Description = "No existing progress record → AddAsync called, Update NOT called",
                ExpectedResult = "IsSuccess=true, AddAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetByUserIdAndTopicIdAsync returns null",
                    "AddAsync called once",
                    "Update never called"
                }
            });
        }

        // ── TC-03: Đã có progress → Update được gọi ──────────────────────
        [Fact]
        public async Task Handle_ExistingProgress_ShouldUpdateRecord()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-001", UserId = "USER-001" };

            var existingProgress = new Domain.Entities.UserTopicProgress
            {
                UserTopicProgressId = "PROG-001",
                UserId = "USER-001",
                TopicId = "TOPIC-001",
                IsLearned = false
            };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-001"))
                         .ReturnsAsync(new Topic { TopicId = "TOPIC-001" });

            var mockProgressRepo = new Mock<IUserTopicProgressRepository>();
            mockProgressRepo.Setup(x => x.GetByUserIdAndTopicIdAsync("USER-001", "TOPIC-001"))
                            .ReturnsAsync(existingProgress);
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = CreateHandler(progressRepo: mockProgressRepo, topicRepo: mockTopicRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingProgress.IsLearned.Should().BeTrue();

            mockProgressRepo.Verify(x => x.Update(existingProgress), Times.Once);
            mockProgressRepo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()), Times.Never);

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_03",
                Description = "Existing progress record found → Update called, IsLearned set to true",
                ExpectedResult = "IsSuccess=true, Update called once, IsLearned=true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetByUserIdAndTopicIdAsync returns existing record",
                    "Update called once",
                    "AddAsync never called",
                    "IsLearned = true"
                }
            });
        }

        // ── TC-04: SaveChangesAsync ném exception → 500 ──────────────────
        [Fact]
        public async Task Handle_SaveChangesFails_ShouldReturn500()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-001", UserId = "USER-001" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-001"))
                         .ReturnsAsync(new Topic { TopicId = "TOPIC-001" });

            var mockProgressRepo = new Mock<IUserTopicProgressRepository>();
            mockProgressRepo.Setup(x => x.GetByUserIdAndTopicIdAsync("USER-001", "TOPIC-001"))
                            .ReturnsAsync((Domain.Entities.UserTopicProgress?)null);
            mockProgressRepo.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()))
                            .Returns(Task.CompletedTask);
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new Exception("DB connection failed"));

            var handler = CreateHandler(progressRepo: mockProgressRepo, topicRepo: mockTopicRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_04",
                Description = "SaveChangesAsync throws exception → return 500",
                ExpectedResult = "IsSuccess=false, StatusCode=500",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SaveChangesAsync throws Exception",
                    "Return 500 with error detail"
                }
            });
        }

        // ── TC-05: IsLearned được set true & CompletedAt được gán ────────
        [Fact]
        public async Task Handle_NewProgress_ShouldSetIsLearnedTrueAndCompletedAt()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-002", UserId = "USER-002" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-002"))
                         .ReturnsAsync(new Topic { TopicId = "TOPIC-002" });

            Domain.Entities.UserTopicProgress? captured = null;
            var mockProgressRepo = new Mock<IUserTopicProgressRepository>();
            mockProgressRepo.Setup(x => x.GetByUserIdAndTopicIdAsync("USER-002", "TOPIC-002"))
                            .ReturnsAsync((Domain.Entities.UserTopicProgress?)null);
            mockProgressRepo.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()))
                            .Callback<Domain.Entities.UserTopicProgress>(p => captured = p)
                            .Returns(Task.CompletedTask);
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = CreateHandler(progressRepo: mockProgressRepo, topicRepo: mockTopicRepo);

            // Act
            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = await handler.Handle(command, CancellationToken.None);
            var after = DateTime.UtcNow.AddSeconds(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.IsLearned.Should().BeTrue();
            captured.CompletedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            captured.UserId.Should().Be("USER-002");
            captured.TopicId.Should().Be("TOPIC-002");

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_05",
                Description = "New progress record has IsLearned=true and CompletedAt set to current UTC time",
                ExpectedResult = "IsLearned=true, CompletedAt within acceptable range",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "New progress captured via Callback",
                    "IsLearned = true",
                    "CompletedAt ≈ UtcNow"
                }
            });
        }

        // ── TC-06: Gọi lại sau khi đã completed (idempotent) ─────────────
        [Fact]
        public async Task Handle_AlreadyCompleted_ShouldUpdateAndReturn200()
        {
            // Arrange
            var command = new CompleteTopicCommand { TopicId = "TOPIC-003", UserId = "USER-003" };

            var alreadyCompleted = new Domain.Entities.UserTopicProgress
            {
                UserTopicProgressId = "PROG-DONE",
                UserId = "USER-003",
                TopicId = "TOPIC-003",
                IsLearned = true,
                CompletedAt = DateTime.UtcNow.AddDays(-3)
            };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-003"))
                         .ReturnsAsync(new Topic { TopicId = "TOPIC-003" });

            var mockProgressRepo = new Mock<IUserTopicProgressRepository>();
            mockProgressRepo.Setup(x => x.GetByUserIdAndTopicIdAsync("USER-003", "TOPIC-003"))
                            .ReturnsAsync(alreadyCompleted);
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = CreateHandler(progressRepo: mockProgressRepo, topicRepo: mockTopicRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            alreadyCompleted.IsLearned.Should().BeTrue();

            mockProgressRepo.Verify(x => x.Update(alreadyCompleted), Times.Once);

            QACollector.LogTestCase("User Topic Progress - Complete", new TestCaseDetail
            {
                FunctionGroup = "CompleteTopic",
                TestCaseID = "CompleteTopic_06",
                Description = "Topic already completed → calling again is idempotent, Update called, return 200",
                ExpectedResult = "IsSuccess=true, Update called once, IsLearned still true",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Existing record with IsLearned=true",
                    "Update called once (idempotent)",
                    "Return 200"
                }
            });
        }
    }
}
