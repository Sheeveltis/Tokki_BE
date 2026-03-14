using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicOrderIndexCommandHandlerTests
    {
        private UpdateTopicOrderIndexCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null)
        {
            return new UpdateTopicOrderIndexCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                new Mock<ILogger<UpdateTopicOrderIndexCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new UpdateTopicOrderIndexCommand
            {
                TopicId = "TOPIC-INVALID",
                OrderIndex = 3,
                UpdatedBy = "ADMIN-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - UpdateOrderIndex", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Order Index",
                TestCaseID = "TC-TOPIC-ORD-01",
                Description = "Update OrderIndex với TopicId không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_SameOrderIndex_ShouldReturnIdempotent200()
        {
            var topic = MockTopicRepository.GetSampleTopic();
            topic.OrderIndex = 3;

            var command = new UpdateTopicOrderIndexCommand
            {
                TopicId = "TOPIC-001",
                OrderIndex = 3, // same as current
                UpdatedBy = "ADMIN-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: topic));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("không thay đổi");

            QACollector.LogTestCase("Topic - UpdateOrderIndex", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Order Index",
                TestCaseID = "TC-TOPIC-ORD-02",
                Description = "Update OrderIndex với giá trị giống hiện tại → idempotent, không thay đổi",
                ExpectedResult = "Return 200, message 'không thay đổi'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "NewOrderIndex == OldOrderIndex (boundary: same value)",
                    "Idempotent → return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidNewOrderIndex_ShouldShiftAndReturn200()
        {
            var topic = MockTopicRepository.GetSampleTopic();
            topic.OrderIndex = 2;

            var command = new UpdateTopicOrderIndexCommand
            {
                TopicId = "TOPIC-001",
                OrderIndex = 5, // move from 2 → 5
                UpdatedBy = "ADMIN-001"
            };

            var mockTopicRepo = MockTopicRepository.GetMock(returnedTopic: topic);

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.OrderIndex.Should().Be(5);

            // ShiftOrderIndexBetweenAsync phải được gọi vì oldIndex có giá trị
            mockTopicRepo.Verify(x => x.ShiftOrderIndexBetweenAsync(
                2, 5,
                It.IsAny<TopicType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()), Times.Once);

            QACollector.LogTestCase("Topic - UpdateOrderIndex", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Order Index",
                TestCaseID = "TC-TOPIC-ORD-03",
                Description = "Di chuyển topic từ vị trí 2 → 5 → ShiftBetween được gọi, OrderIndex cập nhật",
                ExpectedResult = "OrderIndex = 5, ShiftBetween called once, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OldOrderIndex = 2, NewOrderIndex = 5",
                    "ShiftOrderIndexBetweenAsync called",
                    "Return 200"
                }
            });
        }
    }
}