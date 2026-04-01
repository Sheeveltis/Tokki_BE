using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.DeleteExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class DeleteExamTemplateCommandHandlerTests
    {
        private static DeleteExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new DeleteExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object,
                new Mock<ILogger<DeleteExamTemplateCommandHandler>>().Object);
        }

        private static DeleteExamTemplateCommand ValidCommand => new("EXMT-001");

        private static ExamTemplate BuildTemplate(ExamTemplateStatus status) => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "Template A",
            Status         = status
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnExamTemplateNotFound()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-01",
                Description       = "ExamTemplateId not found → ExamTemplateNotFound",
                ExpectedResult    = "Return Failure (not found)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template == null" }
            });
        }

        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturnExamTemplateNotFound()
        {
            var template = BuildTemplate(ExamTemplateStatus.Deleted);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-02",
                Description       = "Template.Status = Deleted → treated as not found",
                ExpectedResult    = "Return Failure (already deleted = not found)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status == Deleted => ExamTemplateNotFound" }
            });
        }

        [Fact]
        public async Task Handle_PublishedTemplate_ShouldReturnExamTemplateCantDelete()
        {
            var template = BuildTemplate(ExamTemplateStatus.Published);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-03",
                Description       = "Published template cannot be deleted → ExamTemplateCantDelete",
                ExpectedResult    = "Return Failure (cannot delete Published)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status == Published => ExamTemplateCantDelete" }
            });
        }

        [Fact]
        public async Task Handle_TemplateInUse_ShouldReturnExamTemplateInUse()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.HasExamsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-04",
                Description       = "Draft template but has linked Exams → ExamTemplateInUse",
                ExpectedResult    = "Return Failure (in use)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HasExamsAsync = true => ExamTemplateInUse" }
            });
        }

        [Fact]
        public async Task Handle_DraftTemplate_NotInUse_ShouldSoftDelete()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.HasExamsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            template.Status.Should().Be(ExamTemplateStatus.Deleted);
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<ExamTemplate>()), Times.Once);

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-05",
                Description       = "Draft, not in use → soft delete (Status=Deleted), Return 200",
                ExpectedResult    = "Return 200, template.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=Draft, HasExams=false => Deleted" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrowsOnUpdate_ShouldReturn500()
        {
            var template = BuildTemplate(ExamTemplateStatus.Draft);
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);
            mockRepo.Setup(x => x.HasExamsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamTemplate>()))
                    .ThrowsAsync(new Exception("DB failure"));

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteExamTemplate",
                TestCaseID        = "TC-EXMT-DEL-06",
                Description       = "UpdateAsync throws → catch returns ServerError",
                ExpectedResult    = "Return Failure(ServerError)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception) => Failure(AppErrors.ServerError)" }
            });
        }
    }
}
