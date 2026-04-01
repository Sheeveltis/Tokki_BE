using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.ExamTemplates.Commands.RejectExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class RejectExamTemplateCommandHandlerTests
    {
        private static RejectExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null)
        {
            return new RejectExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                new Mock<ILogger<RejectExamTemplateCommandHandler>>().Object);
        }

        private static RejectExamTemplateCommand ValidCommand => new()
        {
            ExamTemplateId = "EXMT-001",
            Reason         = "Nội dung chưa đạt yêu cầu"
        };

        private static ExamTemplate BuildTemplate(ExamTemplateStatus status, string createdBy = "TEACHER-001") => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "Test Template",
            Status         = status,
            CreatedBy      = createdBy
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-01",
                Description       = "Template not found → Failure 'Không tìm thấy đề thi mẫu'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_NotPendingApprovalStatus_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chờ duyệt");

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-02",
                Description       = "Template is Draft (not PendingApproval) → Failure 'không ở trạng thái chờ duyệt'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != PendingApproval" }
            });
        }

        [Fact]
        public async Task Handle_EmptyReason_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var cmd = new RejectExamTemplateCommand { ExamTemplateId = "EXMT-001", Reason = "" };
            var result = await CreateHandler(repo: mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("lý do");

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-03",
                Description       = "Reason is empty → Failure 'Vui lòng nhập lý do từ chối'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrWhiteSpace(request.Reason)" }
            });
        }

        [Fact]
        public async Task Handle_ValidReject_ShouldSetStatusRejected()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("TEACHER-001")).ReturnsAsync((Account?)null);

            var result = await CreateHandler(repo: mockRepo, accountRepo: mockAccount).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Rejected);

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-04",
                Description       = "PendingApproval + valid reason → Status = Rejected, Return Success",
                ExpectedResult    = "Return Success(true), Status = Rejected",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Rejected, UpdateAsync called" }
            });
        }

        [Fact]
        public async Task Handle_ValidReject_WithCreatorEmail_ShouldSendRejectionEmail()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval, "TEACHER-001");
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var creator = new Account { UserId = "TEACHER-001", Email = "teacher@tokki.com", FullName = "Nguyen Van A" };
            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("TEACHER-001")).ReturnsAsync(creator);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendExamTemplateRejectedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await CreateHandler(repo: mockRepo, accountRepo: mockAccount, emailService: mockEmail)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockEmail.Verify(x => x.SendExamTemplateRejectedAsync(
                "teacher@tokki.com", "Nguyen Van A", "Test Template", "Nội dung chưa đạt yêu cầu"), Times.Once);

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-05",
                Description       = "Creator has email → SendExamTemplateRejectedAsync called with correct params",
                ExpectedResult    = "Return Success, email sent once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "creator.Email not empty => SendExamTemplateRejectedAsync" }
            });
        }

        [Fact]
        public async Task Handle_EmailThrows_ShouldStillSucceed()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval, "TEACHER-001");
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var creator = new Account { UserId = "TEACHER-001", Email = "teacher@tokki.com", FullName = "Teacher" };
            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("TEACHER-001")).ReturnsAsync(creator);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendExamTemplateRejectedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP error"));

            var result = await CreateHandler(repo: mockRepo, accountRepo: mockAccount, emailService: mockEmail)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); // email failure is swallowed

            QACollector.LogTestCase("Exam Template - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectExamTemplate",
                TestCaseID        = "TC-EXMT-REJ-06",
                Description       = "Email sending throws → swallowed by try/catch, rejection still succeeds",
                ExpectedResult    = "Return Success(true) despite email failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception emailEx) => LogError, continue" }
            });
        }
    }
}
