using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class CreateEmailCampaignByGroupCommandHandlerTests
    {
        private CreateEmailCampaignByGroupCommandHandler CreateHandler(
            Mock<IEmailJobRepository>? repo = null)
        {
            return new CreateEmailCampaignByGroupCommandHandler(
                (repo ?? new Mock<IEmailJobRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_ValidCampaign_ShouldReturn200WithJobId()
        {
            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Thông báo",
                Body = "Nội dung",
                TargetGroup = UserTargetGroup.All,
                CreatedBy = "ADMIN-001"
            };

            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNullOrEmpty();

            mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailJob>()), Times.Once);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup = "Create Email Campaign",
                TestCaseID = "TC-ECMP-CRE-01",
                Description = "Tạo email campaign hợp lệ → lưu job và trả về JobId",
                ExpectedResult = "Return 200, Data = JobId",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Subject, Body, TargetGroup",
                    "AddAsync called once",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_WithScheduledTime_ShouldUseThatTime()
        {
            var scheduledTime = DateTimeOffset.UtcNow.AddHours(7).AddDays(1);

            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Thông báo",
                Body = "Nội dung",
                TargetGroup = UserTargetGroup.All,                CreatedBy = "ADMIN-001",
                ScheduledTime = scheduledTime
            };

            EmailJob? capturedJob = null;
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                    .Callback<EmailJob>(j => capturedJob = j)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedJob.Should().NotBeNull();
            capturedJob!.ScheduledTime.Should().BeCloseTo(
                scheduledTime.ToOffset(TimeSpan.FromHours(7)).DateTime,
                TimeSpan.FromMinutes(1));

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup = "Create Email Campaign",
                TestCaseID = "TC-ECMP-CRE-02",
                Description = "Tạo campaign với ScheduledTime cụ thể → ScheduledTime được lưu đúng",
                ExpectedResult = "Return 200, ScheduledTime ≈ scheduledTime",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "ScheduledTime = tomorrow",
                    "ScheduledTime được set đúng trên EmailJob",
                    "Return 200"
                }
            });
        }
    }
}