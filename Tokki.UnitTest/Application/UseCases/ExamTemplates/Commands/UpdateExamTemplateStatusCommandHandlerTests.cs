using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplateStatus;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class UpdateExamTemplateStatusCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly UpdateExamTemplateStatusCommandHandler _handler;

        public UpdateExamTemplateStatusCommandHandlerTests()
        {
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _handler = new UpdateExamTemplateStatusCommandHandler(_mockTemplateRepo.Object);
        }

        // TC-EXT-UTS-01 | A | Template Not Found
        [Fact]
        public async Task Handle_TemplateNotFound_ShouldFail()
        {
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ExamTemplate)null);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy đề thi mẫu.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-01",
                Description = "Returns failure immediately if template record vanishes",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB returns null" }
            });
        }

        // TC-EXT-UTS-02 | N | Fast Return Same Status
        [Fact]
        public async Task Handle_SameStatus_ShouldReturnSuccessWithoutDbHit()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1", Status = ExamTemplateStatus.Draft };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _mockTemplateRepo.Verify(x => x.UpdateAsync(It.IsAny<ExamTemplate>()), Times.Never);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-02",
                Description = "Identical configurations skip expensive repository updates dynamically capturing speed gains",
                ExpectedResult = "Success true and Times.Never",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Initial status == target status" }
            });
        }

        // TC-EXT-UTS-03 | N | Transition Draft -> PendingApproval
        [Fact]
        public async Task Handle_DraftToPending_ShouldUpdateDb()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1", Status = ExamTemplateStatus.PendingApproval };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.PendingApproval);
            _mockTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-03",
                Description = "Transitions draft safely parsing save context",
                ExpectedResult = "Success true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Draft to pending" }
            });
        }

        // TC-EXT-UTS-04 | N | Transition PendingApproval -> Published
        [Fact]
        public async Task Handle_PendingToPublished_ShouldUpdateDb()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.PendingApproval };
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1", Status = ExamTemplateStatus.Published };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Published);
            _mockTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-04",
                Description = "Transitions approval correctly ensuring pipeline execution completes flawlessly",
                ExpectedResult = "Success true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Pending to Published" }
            });
        }

        // TC-EXT-UTS-05 | N | Transition PendingApproval -> Rejected
        [Fact]
        public async Task Handle_PendingToRejected_ShouldUpdateDb()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.PendingApproval };
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1", Status = ExamTemplateStatus.Rejected };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Rejected);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-05",
                Description = "Forces rejection safely",
                ExpectedResult = "Success true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Pending to Rejected" }
            });
        }

        // TC-EXT-UTS-06 | N | Verify DB Save Call
        [Fact]
        public async Task Handle_StateCommits_ShouldInvokeSaveChangesAsync()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplateStatusCommand { ExamTemplateId = "T1", Status = ExamTemplateStatus.PendingApproval };
            await _handler.Handle(command, CancellationToken.None);

            _mockTemplateRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplateStatusCommandHandler",
                TestCaseID = "TC-EXT-UTS-06",
                Description = "Final persistence triggers transaction flush internally resolving entity locks",
                ExpectedResult = "Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync execution" }
            });
        }
    }
}
