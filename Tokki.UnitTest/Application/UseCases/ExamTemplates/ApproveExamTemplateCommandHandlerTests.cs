using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.ApproveExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class ApproveExamTemplateCommandHandlerTests
    {
        private static ApproveExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new ApproveExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static ApproveExamTemplateCommand ValidCommand => new() { ExamTemplateId = "EXMT-001" };

        private static ExamTemplate BuildTemplate(ExamTemplateStatus status) => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "TOEIC Test",
            Status         = status
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-01",
                Description       = "ExamTemplateId not found → Failure 'Không tìm thấy đề thi'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_PendingApprovalTemplate_ShouldSetStatusPublished()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Published);

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-02",
                Description       = "PendingApproval template → Status set to Published",
                ExpectedResult    = "Return Success(true), Status = Published",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate.Status = Published" }
            });
        }

        [Fact]
        public async Task Handle_Approve_ShouldCallUpdateAndSave()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<ExamTemplate>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-03",
                Description       = "Approve calls UpdateAsync and SaveChangesAsync exactly once",
                ExpectedResult    = "UpdateAsync × 1, SaveChangesAsync × 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateAsync + SaveChangesAsync called once" }
            });
        }

        [Fact]
        public async Task Handle_DraftTemplate_ShouldApproveAnyway()
        {
            // Note: handler does NOT check current status before approving
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Published);

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-04",
                Description       = "Draft template (no status guard in Approve handler) → also Published",
                ExpectedResult    = "Return Success, Status = Published",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Handler has no status check → any status approved" }
            });
        }

        [Fact]
        public async Task Handle_ApproveAlreadyPublished_ShouldIdempotentlySucceed()
        {
            var template = BuildTemplate(ExamTemplateStatus.Published);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Published);

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-05",
                Description       = "Already Published → re-approve succeeds idempotently",
                ExpectedResult    = "Return Success, Status stays Published",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Published (boundary: re-approve)" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB down"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveExamTemplate",
                TestCaseID        = "TC-EXMT-APR-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws" }
            });
        }
    }
}
