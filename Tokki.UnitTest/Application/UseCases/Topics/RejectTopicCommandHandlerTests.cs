using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Topics.Commands.RejectTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class RejectTopicCommandHandlerTests
    {
        private RejectTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            bool unauthorized = false)
        {
            return new RejectTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                MockAccountRepository.GetMock().Object,
                new Mock<IEmailService>().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<RejectTopicCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_MissingRejectReason_ShouldReturn400()
        {
            var command = new RejectTopicCommand
            {
                TopicId = "TOPIC-002",
                RejectReason = ""
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Topic",
                TestCaseID = "TC-TOPIC-REJ-01",
                Description = "Reject topic mà không nhập lý do từ chối",
                ExpectedResult = "Return 400 REJECT_REASON_REQUIRED",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "RejectReason = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_TopicNotPendingApproval_ShouldReturn400()
        {
            var command = new RejectTopicCommand
            {
                TopicId = "TOPIC-001",
                RejectReason = "Nội dung không phù hợp"
            };

            // Topic đang Active → không phải PendingApproval
            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic(
                        status: TopicStatus.Active)));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Topic",
                TestCaseID = "TC-TOPIC-REJ-02",
                Description = "Reject topic không ở trạng thái PendingApproval",
                ExpectedResult = "Return 400 TOPIC_INVALID_STATUS",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Active (not PendingApproval)",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingTopic_ShouldSetRejectedAndReturn200()
        {
            var command = new RejectTopicCommand
            {
                TopicId = "TOPIC-002",
                RejectReason = "Nội dung không phù hợp với chương trình học"
            };

            var pendingTopic = MockTopicRepository.GetSampleTopicPendingApproval();

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: pendingTopic));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            pendingTopic.Status.Should().Be(TopicStatus.Rejected);

            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Topic",
                TestCaseID = "TC-TOPIC-REJ-03",
                Description = "Reject topic PendingApproval hợp lệ → Status = Rejected, return 200",
                ExpectedResult = "Status = Rejected, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "RejectReason provided",
                    "Return 200"
                }
            });
        }
    }
}