using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.ResetExamTemplateToDraft;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class ResetExamTemplateToDraftCommandHandlerTests
    {
        private static ResetExamTemplateToDraftCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new ResetExamTemplateToDraftCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static ResetExamTemplateToDraftCommand ValidCommand => new() { ExamTemplateId = "EXMT-001" };

        private static ExamTemplate BuildTemplate(ExamTemplateStatus status) => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "TOEIC Template",
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

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_01",
                Description       = "Template not found → Failure 'Không tìm thấy đề thi mẫu'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_NotRejectedStatus_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Từ chối");

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_02",
                Description       = "Status = Draft (not Rejected) → Failure 'chỉ có thể mở lại khi đã bị Từ chối'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != Rejected" }
            });
        }

        [Fact]
        public async Task Handle_PendingApprovalStatus_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.PendingApproval);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_03",
                Description       = "Status = PendingApproval → cannot reset to draft",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == PendingApproval != Rejected => Failure" }
            });
        }

        [Fact]
        public async Task Handle_RejectedTemplate_ShouldSetStatusDraft()
        {
            var template = BuildTemplate(ExamTemplateStatus.Rejected);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Draft);

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_04",
                Description       = "Rejected template → Status reset to Draft, Return Success",
                ExpectedResult    = "Return Success(true), Status = Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Draft, UpdateAsync called" }
            });
        }

        [Fact]
        public async Task Handle_RejectedTemplate_ShouldCallUpdateAndSave()
        {
            var template = BuildTemplate(ExamTemplateStatus.Rejected);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<ExamTemplate>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_05",
                Description       = "UpdateAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateAsync × 1, SaveChangesAsync × 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Both repository calls invoked once" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB failure"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Reset To Draft", new TestCaseDetail
            {
                FunctionGroup     = "ResetExamTemplateToDraft",
                TestCaseID        = "ResetExamTemplateToDraft_06",
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
