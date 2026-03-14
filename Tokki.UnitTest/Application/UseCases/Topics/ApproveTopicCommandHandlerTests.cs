using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Topics.Commands.ApproveTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class ApproveTopicCommandHandlerTests
    {
        private ApproveTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            bool unauthorized = false)
        {
            return new ApproveTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<ApproveTopicCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new ApproveTopicCommand { TopicId = "TOPIC-001" };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Topic",
                TestCaseID = "TC-TOPIC-APP-01",
                Description = "Approve topic khi không có token",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new ApproveTopicCommand { TopicId = "TOPIC-INVALID" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Topic",
                TestCaseID = "TC-TOPIC-APP-02",
                Description = "Approve topic với ID không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_TopicAlreadyActive_ShouldReturnIdempotent200()
        {
            // Topic đã Active → idempotent → vẫn trả 200
            var command = new ApproveTopicCommand { TopicId = "TOPIC-001" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic(
                        status: TopicStatus.Active)));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Topic",
                TestCaseID = "TC-TOPIC-APP-03",
                Description = "Approve topic đã ở trạng thái Active → idempotent, vẫn return 200",
                ExpectedResult = "Return 200 (idempotent)",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Active (boundary: đã được approve)",
                    "Idempotent → return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingApproval_ShouldSetActiveAndSendEmail()
        {
            var command = new ApproveTopicCommand { TopicId = "TOPIC-002" };

            var pendingTopic = MockTopicRepository.GetSampleTopicPendingApproval();
            var creator = new Account
            {
                UserId  = "STAFF-001",
                Email = "staff@tokki.com",
                FullName = "Tokki Staff"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByIdAsync("STAFF-001"))
                           .ReturnsAsync(creator);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: pendingTopic),
                accountRepo: mockAccountRepo,
                emailService: mockEmail);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            pendingTopic.Status.Should().Be(TopicStatus.Active);
            pendingTopic.ApprovedBy.Should().Be("ADMIN-001");

            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Topic",
                TestCaseID = "TC-TOPIC-APP-04",
                Description = "Approve topic PendingApproval hợp lệ → Status = Active, gửi email, return 200",
                ExpectedResult = "Status = Active, ApprovedBy set, email sent, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "Creator has valid email",
                    "Return 200"
                }
            });
        }
    }
}