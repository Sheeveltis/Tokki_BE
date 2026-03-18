using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class PublishTopicCommandHandlerTests
    {
        private PublishTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            bool unauthorized = false)
        {
            return new PublishTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new PublishTopicCommandValidator());
        }

        [Fact]
        public async Task Handle_TopicNotDraft_ShouldReturn400()
        {
            var command = new PublishTopicCommand { TopicId = "TOPIC-002" };

            // PendingApproval → không phải Draft → invalid transition
            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopicPendingApproval()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail
            {
                FunctionGroup = "Publish Topic",
                TestCaseID = "TC-TOPIC-PUB-01",
                Description = "Publish topic không ở trạng thái Draft (PendingApproval)",
                ExpectedResult = "Return 400 TopicInvalidStatusTransition",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "Invalid transition to Active",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidDraftTopic_ShouldSetActiveWithOrderIndexAndReturn200()
        {
            var command = new PublishTopicCommand { TopicId = "TOPIC-003" };

            var draftTopic = MockTopicRepository.GetSampleTopicDraft();

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: draftTopic,
                    maxOrderIndex: 5)); // maxOrderIndex = 5 → newOrderIndex = 6

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            draftTopic.Status.Should().Be(TopicStatus.Active);
            draftTopic.OrderIndex.Should().Be(6);
            draftTopic.ApprovedBy.Should().Be("ADMIN-001");

            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail
            {
                FunctionGroup = "Publish Topic",
                TestCaseID = "TC-TOPIC-PUB-02",
                Description = "Publish topic Draft hợp lệ → Status = Active, OrderIndex = maxOrderIndex + 1",
                ExpectedResult = "Status = Active, OrderIndex = 6, ApprovedBy set, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Draft",
                    "MaxOrderIndex = 5 → newOrderIndex = 6",
                    "Return 200"
                }
            });
        }
    }
}