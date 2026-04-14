using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.SubmitExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class SubmitExamTemplateCommandHandlerTests
    {
        private static SubmitExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new SubmitExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static SubmitExamTemplateCommand ValidCommand => new() { ExamTemplateId = "EXMT-001" };

        private static ExamTemplate BuildTemplate(ExamTemplateStatus status, int partCount = 1) => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "IELTS Template",
            Status         = status,
            TemplateParts  = partCount == 0
                ? new List<TemplatePart>()
                : new List<TemplatePart>
                {
                    new() { TemplatePartId = "PT-001", Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 5 }
                }
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-01",
                Description       = "Template not found → Failure",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_PublishedStatus_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.Published);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Trạng thái hiện tại");

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-02",
                Description       = "Status = Published → 'Trạng thái hiện tại không thể gửi duyệt'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Published → invalid submit state" }
            });
        }

        [Fact]
        public async Task Handle_DraftWithNoParts_ShouldReturnFailure()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft, partCount: 0);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chưa có nội dung");

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-03",
                Description       = "Draft with no parts → 'Đề thi chưa có nội dung phần thi'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts.Count == 0" }
            });
        }

        [Fact]
        public async Task Handle_DraftWithParts_ShouldSetPendingApproval()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft, partCount: 1);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.PendingApproval);

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-04",
                Description       = "Draft with parts → Status = PendingApproval, Return Success",
                ExpectedResult    = "Return Success(true), Status = PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=Draft, Parts.Any() => PendingApproval" }
            });
        }

        [Fact]
        public async Task Handle_RejectedWithParts_ShouldAlsoAllowResubmit()
        {
            var template = BuildTemplate(ExamTemplateStatus.Rejected, partCount: 1);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.PendingApproval);

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-05",
                Description       = "Rejected template with parts → re-submit allowed → PendingApproval",
                ExpectedResult    = "Return Success, Status = PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Rejected → also allowed to submit" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitExamTemplate",
                TestCaseID        = "TC-EXMT-SUB-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithPartsAsync throws" }
            });
        }
    }
}
