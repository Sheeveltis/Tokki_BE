using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.EmailCampaigns.Commands
{
    public class CreateEmailCampaignByGroupCommandHandlerTests
    {
        private readonly Mock<IEmailJobRepository> _mockRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly CreateEmailCampaignByGroupCommandHandler _handler;

        public CreateEmailCampaignByGroupCommandHandlerTests()
        {
            _mockRepo = new Mock<IEmailJobRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _handler = new CreateEmailCampaignByGroupCommandHandler(
                _mockRepo.Object,
                _mockIdGen.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_CreateJob_When_ScheduledTimeIsNull_And_SpecificEmailsNull()
        {
            // Arrange
            var command = new CreateEmailCampaignByGroupCommand
            {
                CreatedBy = "admin-01",
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = null,
                ScheduledTime = null
            };

            _mockIdGen.Setup(x => x.Generate(15)).Returns("job-123");

            EmailJob? addedJob = null;
            _mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => addedJob = j)
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var vnOffset = TimeSpan.FromHours(7);
            var nowVn = DateTimeOffset.UtcNow.ToOffset(vnOffset);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Đã lên lịch gửi email thành công!");
            result.Data.Should().Be("job-123");

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            addedJob.Should().NotBeNull();
            addedJob!.JobId.Should().Be("job-123");
            addedJob.CreatedBy.Should().Be("admin-01");
            addedJob.Subject.Should().Be("Sub");
            addedJob.Body.Should().Be("Body");
            addedJob.TargetGroup.Should().Be(UserTargetGroup.All);
            addedJob.Status.Should().Be(EmailJobStatus.Pending);

            // SpecificEmails null => lưu null
            addedJob.SpecificEmails.Should().BeNull();

            // ScheduledTime null => handler dùng nowVn (giờ VN) => DateTimeKind.Unspecified
            addedJob.ScheduledTime.Kind.Should().Be(DateTimeKind.Unspecified);
            addedJob.ScheduledTime.Should().BeCloseTo(nowVn.DateTime, TimeSpan.FromMinutes(1));

            // CreatedAt set theo nowVn (giờ VN) => DateTimeKind.Unspecified
            addedJob.CreatedAt.Kind.Should().Be(DateTimeKind.Unspecified);
            addedJob.CreatedAt.Should().BeCloseTo(nowVn.DateTime, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Handle_Should_Serialize_SpecificEmails_ToJson_When_ListProvided()
        {
            // Arrange
            var emails = new List<string> { "a@tokki.vn", "b@tokki.vn" };

            var command = new CreateEmailCampaignByGroupCommand
            {
                CreatedBy = "admin-01",
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = emails,
                ScheduledTime = null
            };

            _mockIdGen.Setup(x => x.Generate(15)).Returns("job-456");

            EmailJob? addedJob = null;
            _mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => addedJob = j)
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            addedJob.Should().NotBeNull();

            var json = addedJob!.SpecificEmails;
            json.Should().NotBeNull();

            var parsed = JsonSerializer.Deserialize<List<string>>(json!);
            parsed.Should().NotBeNull();
            parsed!.Should().BeEquivalentTo(emails);
        }

        [Fact]
        public async Task Handle_Should_Convert_ScheduledTime_ToVn_And_Save_AsUnspecifiedDateTime()
        {
            // Arrange
            // Một thời điểm cụ thể, cố tình set offset khác +07:00 để test ToOffset(+07:00)
            var scheduledUtc = DateTimeOffset.UtcNow.AddHours(2); // future
            var scheduledWithOffset0 = new DateTimeOffset(scheduledUtc.UtcDateTime, TimeSpan.Zero);

            var command = new CreateEmailCampaignByGroupCommand
            {
                CreatedBy = "admin-01",
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.VipUsers,
                ScheduledTime = scheduledWithOffset0
            };

            _mockIdGen.Setup(x => x.Generate(15)).Returns("job-789");

            EmailJob? addedJob = null;
            _mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => addedJob = j)
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var vnOffset = TimeSpan.FromHours(7);
            var expectedVn = scheduledWithOffset0.ToOffset(vnOffset).DateTime; // handler lưu DateTime phần VN

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            addedJob.Should().NotBeNull();

            addedJob!.TargetGroup.Should().Be(UserTargetGroup.VipUsers);
            addedJob.ScheduledTime.Kind.Should().Be(DateTimeKind.Unspecified);
            addedJob.ScheduledTime.Should().BeCloseTo(expectedVn, TimeSpan.FromSeconds(2));
        }
    }

    public class CreateEmailCampaignByGroupCommandValidatorTests
    {
        private readonly CreateEmailCampaignByGroupCommandValidator _validator;

        public CreateEmailCampaignByGroupCommandValidatorTests()
        {
            _validator = new CreateEmailCampaignByGroupCommandValidator();
        }

        [Fact]
        public void Validator_Should_Fail_When_Subject_Empty()
        {
            var cmd = new CreateEmailCampaignByGroupCommand
            {
                Subject = "",
                Body = "Body",
                TargetGroup = UserTargetGroup.All
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.Subject);
        }

        [Fact]
        public void Validator_Should_Fail_When_Body_Empty()
        {
            var cmd = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Sub",
                Body = "",
                TargetGroup = UserTargetGroup.All
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.Body);
        }

        [Fact]
        public void Validator_Should_Fail_When_ScheduledTime_InPast_VnTime()
        {
            var vnOffset = TimeSpan.FromHours(7);
            var pastVn = DateTimeOffset.UtcNow.ToOffset(vnOffset).AddMinutes(-1);

            var cmd = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.All,
                ScheduledTime = pastVn
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.ScheduledTime)
                  .WithErrorMessage("Thời gian lên lịch gửi phải lớn hơn thời gian hiện tại (giờ Việt Nam).");
        }

        [Fact]
        public void Validator_Should_Fail_When_SpecificEmails_Contains_InvalidEmail()
        {
            var cmd = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = new List<string> { "a@tokki.vn", "not-an-email" }
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.SpecificEmails)
                  .WithErrorMessage("Danh sách email chứa địa chỉ không hợp lệ");
        }

        [Fact]
        public void Validator_Should_Pass_When_DataValid_And_ScheduledTime_Future()
        {
            var vnOffset = TimeSpan.FromHours(7);
            var futureVn = DateTimeOffset.UtcNow.ToOffset(vnOffset).AddMinutes(10);

            var cmd = new CreateEmailCampaignByGroupCommand
            {
                Subject = "Sub",
                Body = "Body",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = new List<string> { "a@tokki.vn" },
                ScheduledTime = futureVn
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
