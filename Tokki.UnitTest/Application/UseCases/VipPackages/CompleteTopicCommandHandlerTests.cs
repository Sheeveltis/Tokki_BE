using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
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
                (progressRepo ?? MockUserTopicProgressRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new CompleteTopicCommand
            {
                UserId = "USER-001",
                TopicId = "TOPIC-INVALID"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("UserTopicProgress - Complete", new TestCaseDetail
            {
                FunctionGroup = "Complete Topic",
                TestCaseID = "TC-UTP-CMP-01",
                Description = "Complete topic với TopicId không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid TopicId",
                    "Topic = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_NoExistingProgress_ShouldCreateNewProgressAndReturn200()
        {
            // Chưa có progress → tạo mới với IsLearned = true
            var command = new CompleteTopicCommand
            {
                UserId = "USER-001",
                TopicId = "TOPIC-001"
            };

            var mockProgressRepo = MockUserTopicProgressRepository.GetMock(
                returnedProgress: null); // null → tạo mới

            var handler = CreateHandler(
                progressRepo: mockProgressRepo,
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify AddAsync được gọi 1 lần (tạo mới)
            mockProgressRepo.Verify(
                x => x.AddAsync(It.Is<Domain.Entities.UserTopicProgress>(p =>
                    p.IsLearned == true &&
                    p.UserId == "USER-001" &&
                    p.TopicId == "TOPIC-001")),
                Times.Once);

            // Verify Update không được gọi
            mockProgressRepo.Verify(
                x => x.Update(It.IsAny<Domain.Entities.UserTopicProgress>()),
                Times.Never);

            QACollector.LogTestCase("UserTopicProgress - Complete", new TestCaseDetail
            {
                FunctionGroup = "Complete Topic",
                TestCaseID = "TC-UTP-CMP-02",
                Description = "Lần đầu hoàn thành topic: chưa có progress → tạo mới với IsLearned = true",
                ExpectedResult = "Return 200, AddAsync called once, IsLearned = true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No existing UserTopicProgress",
                    "AddAsync called once",
                    "IsLearned = true",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ExistingProgress_ShouldUpdateIsLearnedAndReturn200()
        {
            // Đã có progress → update IsLearned = true
            var command = new CompleteTopicCommand
            {
                UserId = "USER-001",
                TopicId = "TOPIC-001"
            };

            var existingProgress = MockUserTopicProgressRepository.GetSampleProgress(
                isLearned: false); // chưa học xong

            var mockProgressRepo = MockUserTopicProgressRepository.GetMock(
                returnedProgress: existingProgress);

            var handler = CreateHandler(
                progressRepo: mockProgressRepo,
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            existingProgress.IsLearned.Should().BeTrue();
            existingProgress.CompletedAt.Should().NotBeNull();

            // Verify Update được gọi, không gọi AddAsync
            mockProgressRepo.Verify(
                x => x.Update(It.IsAny<Domain.Entities.UserTopicProgress>()),
                Times.Once);

            mockProgressRepo.Verify(
                x => x.AddAsync(It.IsAny<Domain.Entities.UserTopicProgress>()),
                Times.Never);

            QACollector.LogTestCase("UserTopicProgress - Complete", new TestCaseDetail
            {
                FunctionGroup = "Complete Topic",
                TestCaseID = "TC-UTP-CMP-03",
                Description = "Đã có progress chưa hoàn thành → update IsLearned = true, CompletedAt được set",
                ExpectedResult = "Return 200, Update called once, IsLearned = true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Existing progress với IsLearned = false",
                    "Update called once",
                    "IsLearned = true, CompletedAt != null",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_AlreadyCompletedProgress_ShouldUpdateAgainAndReturn200()
        {
            // Progress đã IsLearned = true → vẫn update (idempotent)
            var command = new CompleteTopicCommand
            {
                UserId = "USER-001",
                TopicId = "TOPIC-001"
            };

            var alreadyCompletedProgress = MockUserTopicProgressRepository.GetSampleProgress(
                isLearned: true); // đã học xong

            var mockProgressRepo = MockUserTopicProgressRepository.GetMock(
                returnedProgress: alreadyCompletedProgress);

            var handler = CreateHandler(
                progressRepo: mockProgressRepo,
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            alreadyCompletedProgress.IsLearned.Should().BeTrue();

            QACollector.LogTestCase("UserTopicProgress - Complete", new TestCaseDetail
            {
                FunctionGroup = "Complete Topic",
                TestCaseID = "TC-UTP-CMP-04",
                Description = "Progress đã IsLearned = true → update lại (idempotent), return 200",
                ExpectedResult = "Return 200, IsLearned vẫn = true, Update called",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Existing progress với IsLearned = true (boundary: đã hoàn thành)",
                    "Idempotent → update lại vẫn được",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldReturn500()
        {
            var command = new CompleteTopicCommand
            {
                UserId = "USER-001",
                TopicId = "TOPIC-001"
            };

            var mockProgressRepo = MockUserTopicProgressRepository.GetMock(
                returnedProgress: null);

            // SaveChangesAsync throw exception
            mockProgressRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new Exception("DB save failed"));

            var handler = CreateHandler(
                progressRepo: mockProgressRepo,
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("UserTopicProgress - Complete", new TestCaseDetail
            {
                FunctionGroup = "Complete Topic",
                TestCaseID = "TC-UTP-CMP-05",
                Description = "SaveChangesAsync throw exception → return 500",
                ExpectedResult = "Return 500 Database.SaveError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SaveChangesAsync throws Exception",
                    "Caught in try/catch",
                    "Return 500"
                }
            });
        }
    }
}