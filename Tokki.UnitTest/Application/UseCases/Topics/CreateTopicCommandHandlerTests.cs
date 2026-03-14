using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CreateTopicCommandHandlerTests
    {
        private CreateTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            bool unauthorized = false)
        {
            return new CreateTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                new Mock<ILogger<CreateTopicCommandHandler>>().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new CreateTopicCommand
            {
                TopicName = "Chào hỏi",
                Level = TopicLevel.Level1
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Topic - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Topic",
                TestCaseID = "TC-TOPIC-CRE-01",
                Description = "Tạo topic khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateTopicName_ShouldReturn409()
        {
            var command = new CreateTopicCommand
            {
                TopicName = "Chào hỏi cơ bản",
                Level = TopicLevel.Level1
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(isTopicNameExists: true));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Topic - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Topic",
                TestCaseID = "TC-TOPIC-CRE-02",
                Description = "Tạo topic với tên đã tồn tại trong hệ thống",
                ExpectedResult = "Return 409 TopicNameDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TopicName đã tồn tại",
                    "Return 409"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn201WithDraftStatus()
        {
            var command = new CreateTopicCommand
            {
                TopicName = "Topic Mới",
                Level = TopicLevel.Level1,
                Description = "Mô tả topic mới"
            };

            var mockTopicRepo = MockTopicRepository.GetMock(isTopicNameExists: false);
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateTopicCommandHandler>>();

            mockIdGen.Setup(x => x.GenerateCustom(15)).Returns("TOPIC-NEW-001");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "USER-001")
    };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var handler = new CreateTopicCommandHandler(
                mockTopicRepo.Object,
                mockIdGen.Object,
                mockLogger.Object,
                mockHttpContextAccessor.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("TOPIC-NEW-001");

            mockTopicRepo.Verify(x => x.AddAsync(It.IsAny<Topic>()), Times.Once);
            mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Topic - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Topic",
                TestCaseID = "TC-TOPIC-CRE-03",
                Description = "Tạo topic hợp lệ → Status = Draft, trả về TopicId",
                ExpectedResult = "Return 201, Data = TopicId",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
        {
            "Valid TopicName",
            "Authenticated User",
            "No duplicate",
            "Status = Draft",
            "Return 201"
        }
            });
        }
    }
}