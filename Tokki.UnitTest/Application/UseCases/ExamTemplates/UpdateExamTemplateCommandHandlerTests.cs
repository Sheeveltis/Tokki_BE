using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class UpdateExamTemplateCommandHandlerTests
    {
        private static UpdateExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new UpdateExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static ExamTemplate DraftTemplate(string id = "EXMT-001") => new()
        {
            ExamTemplateId = id,
            Name           = "Old Name",
            Description    = "Old Desc",
            Type           = ExamType.TopikI,
            Status         = ExamTemplateStatus.Draft
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(
                new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-01",
                Description       = "Template not found → Failure",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "examTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_NotDraftStatus_ShouldReturnFailure()
        {
            var template = new ExamTemplate
            {
                ExamTemplateId = "EXMT-001",
                Status         = ExamTemplateStatus.PendingApproval
            };
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(
                new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001", Name = "New Name" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Draft");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-02",
                Description       = "Status = PendingApproval (not Draft) → Failure 'Chỉ được sửa khi Draft'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != Draft" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateNewName_ShouldReturnFailure()
        {
            var template = DraftTemplate();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.IsNameExistsAsync("Taken Name", "EXMT-001")).ReturnsAsync(true);

            var cmd = new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001", Name = "Taken Name" };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("tồn tại");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-03",
                Description       = "New name already used by another template → Failure 'Tên đề thi đã tồn tại'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync(newName, excludeId) = true" }
            });
        }

        [Fact]
        public async Task Handle_ValidNameUpdate_ShouldPersistName()
        {
            var template = DraftTemplate();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.IsNameExistsAsync("New Name", "EXMT-001")).ReturnsAsync(false);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001", Name = "New Name" };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Name.Should().Be("New Name");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-04",
                Description       = "Valid new name → template.Name updated, Return Success",
                ExpectedResult    = "Return Success(true), Name = 'New Name'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Name unique → template.Name = request.Name" }
            });
        }

        [Fact]
        public async Task Handle_ValidTypeUpdate_ShouldPersistType()
        {
            var template = DraftTemplate();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001", Type = ExamType.TopikII };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Type.Should().Be(ExamType.TopikII);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-05",
                Description       = "Type changed to IELTS → template.Type updated",
                ExpectedResult    = "Return Success, template.Type = IELTS",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Type.HasValue => template.Type = IELTS" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(
                    new UpdateExamTemplateCommand { ExamTemplateId = "EXMT-001" }, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateExamTemplate",
                TestCaseID        = "TC-EXMT-UPD-06",
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
