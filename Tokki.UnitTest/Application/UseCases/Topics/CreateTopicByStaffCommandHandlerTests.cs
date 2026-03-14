using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CreateTopicByStaffCommandHandlerTests
    {
        private CreateTopicByStaffCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            bool unauthorized = false)
        {
            return new CreateTopicByStaffCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("STAFF-001").Object,
                new Mock<ILogger<CreateTopicByStaffCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_DuplicateTopicName_ShouldReturn409()
        {
            var command = new CreateTopicByStaffCommand
            {
                TopicName = "Chào hỏi cơ bản",
                Level = TopicLevel.Level1
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(isTopicNameExists: true));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Create Topic By Staff",
                TestCaseID = "TC-TOPIC-STA-01",
                Description = "Staff tạo topic với tên đã tồn tại",
                ExpectedResult = "Return 409 TopicNameDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TopicName duplicate",
                    "Return 409"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn201WithPendingApprovalStatus()
        {
            var command = new CreateTopicByStaffCommand
            {
                TopicName = "Topic Staff Mới",
                Level = TopicLevel.Level3
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(isTopicNameExists: false));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Message.Should().Contain("chờ phê duyệt");

            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail
            {
                FunctionGroup = "Create Topic By Staff",
                TestCaseID = "TC-TOPIC-STA-02",
                Description = "Staff tạo topic hợp lệ → Status = PendingApproval, chờ duyệt",
                ExpectedResult = "Return 201, message chứa 'chờ phê duyệt'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Staff role",
                    "No duplicate",
                    "Status = PendingApproval",
                    "Return 201"
                }
            });
        }
    }
}