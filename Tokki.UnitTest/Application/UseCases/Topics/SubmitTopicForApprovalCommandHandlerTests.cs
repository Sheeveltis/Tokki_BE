using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class SubmitTopicForApprovalCommandHandlerTests
    {
        private SubmitTopicForApprovalCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            bool unauthorized = false)
        {
            return new SubmitTopicForApprovalCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("STAFF-001").Object,
                new Mock<ILogger<SubmitTopicForApprovalCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_TopicNotDraft_ShouldReturn400()
        {
            var command = new SubmitTopicForApprovalCommand { TopicId = "TOPIC-001" };

            // Topic đang Active → không phải Draft
            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic(
                        status: TopicStatus.Active)));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup = "Submit Topic For Approval",
                TestCaseID = "TC-TOPIC-SFA-01",
                Description = "Submit topic đang ở trạng thái Active (không phải Draft)",
                ExpectedResult = "Return 400 TOPIC_INVALID_STATUS",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Active",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidDraftTopic_ShouldSetPendingApprovalAndReturn200()
        {
            var command = new SubmitTopicForApprovalCommand { TopicId = "TOPIC-003" };

            var draftTopic = MockTopicRepository.GetSampleTopicDraft();

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: draftTopic));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            draftTopic.Status.Should().Be(TopicStatus.PendingApproval);

            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup = "Submit Topic For Approval",
                TestCaseID = "TC-TOPIC-SFA-02",
                Description = "Submit topic Draft hợp lệ → Status = PendingApproval, return 200",
                ExpectedResult = "Status = PendingApproval, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Draft",
                    "Valid UserId",
                    "Return 200"
                }
            });
        }
    }
}