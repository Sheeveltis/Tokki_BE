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
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class DuplicateExamTemplateCommandHandlerTests
    {
        private static DuplicateExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? examRepo = null,
            Mock<ITemplatePartRepository>? partRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new DuplicateExamTemplateCommandHandler(
                (examRepo ?? new Mock<IExamTemplateRepository>()).Object,
                (partRepo ?? new Mock<ITemplatePartRepository>()).Object,
                MockIdGeneratorService.GetMock().Object,
                new Mock<ILogger<DuplicateExamTemplateCommandHandler>>().Object,
                (httpCtx ?? new Mock<IHttpContextAccessor>()).Object);
        }

        private static ExamTemplate BuildTemplateWithParts(string id = "EXMT-001") => new()
        {
            ExamTemplateId = id,
            Name           = "Original Template",
            Description    = "Desc",
            Type           = ExamType.TopikI,
            Status         = ExamTemplateStatus.Published,
            TemplateParts  = new List<TemplatePart>
            {
                new() { TemplatePartId = "PT-001", Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 10, Mark = 5 }
            }
        };

        private static DuplicateExamTemplateCommand ValidCommand => new("EXMT-001");

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnExamTemplateNotFound()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(examRepo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-01",
                Description       = "Source template not found → ExamTemplateNotFound",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "originalTemplate == null" }
            });
        }

        [Fact]
        public async Task Handle_ValidTemplate_ShouldCreateCopyWithDraftStatus()
        {
            var original = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(original);
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockPartRepo = new Mock<ITemplatePartRepository>();
            mockPartRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<TemplatePart>>())).Returns(Task.CompletedTask);

            ExamTemplate? captured = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => captured = t)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(examRepo: mockRepo, partRepo: mockPartRepo)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            captured!.Status.Should().Be(ExamTemplateStatus.Draft);
            captured.Name.Should().Contain("Original Template");

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-02",
                Description       = "Valid template → copy created with Status=Draft, name suffixed '(1)'",
                ExpectedResult    = "Return 201, Status=Draft, Name contains original name",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newTemplate.Status = Draft", "Name = '...Original Template (1)'" }
            });
        }

        [Fact]
        public async Task Handle_TemplateHasParts_ShouldDuplicateParts()
        {
            var original = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(original);
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            List<TemplatePart>? capturedParts = null;
            var mockPartRepo = new Mock<ITemplatePartRepository>();
            mockPartRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<TemplatePart>>()))
                        .Callback<List<TemplatePart>>(p => capturedParts = p)
                        .Returns(Task.CompletedTask);

            var result = await CreateHandler(examRepo: mockRepo, partRepo: mockPartRepo)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedParts.Should().NotBeNull();
            capturedParts.Should().HaveCount(1);
            capturedParts![0].Skill.Should().Be(QuestionSkill.Listening);

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-03",
                Description       = "Template has 1 part → AddRangeAsync called with 1 duplicated part",
                ExpectedResult    = "Return 201, AddRangeAsync × 1 with 1 part",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts.Any() => AddRangeAsync(newParts)" }
            });
        }

        [Fact]
        public async Task Handle_NoParts_ShouldNotCallAddRange()
        {
            var original = new ExamTemplate
            {
                ExamTemplateId = "EXMT-001",
                Name           = "Empty Template",
                Type           = ExamType.TopikI,
                Status         = ExamTemplateStatus.Draft,
                TemplateParts  = new List<TemplatePart>() // no parts
            };

            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(original);
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockPartRepo = new Mock<ITemplatePartRepository>();

            var result = await CreateHandler(examRepo: mockRepo, partRepo: mockPartRepo)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockPartRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<TemplatePart>>()), Times.Never);

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-04",
                Description       = "Template has no parts → AddRangeAsync never called",
                ExpectedResult    = "Return 201, AddRangeAsync × 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!TemplateParts.Any() => skip AddRangeAsync" }
            });
        }

        [Fact]
        public async Task Handle_NameAlreadyExists_ShouldIncrementSuffix()
        {
            var original = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(original);
            // "Original Template (1)" exists → try "(2)"
            mockRepo.Setup(x => x.IsNameExistsAsync("Original Template (1)", null)).ReturnsAsync(true);
            mockRepo.Setup(x => x.IsNameExistsAsync("Original Template (2)", null)).ReturnsAsync(false);

            ExamTemplate? captured = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => captured = t)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockPartRepo = new Mock<ITemplatePartRepository>();
            mockPartRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<TemplatePart>>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(examRepo: mockRepo, partRepo: mockPartRepo)
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            captured!.Name.Should().Be("Original Template (2)");

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-05",
                Description       = "'(1)' name taken → auto-increment to '(2)'",
                ExpectedResult    = "Return 201, Name = 'Original Template (2)'",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync('(1)') = true → suffix = 2" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrowsOnAdd_ShouldReturnServerError()
        {
            var original = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(original);
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .ThrowsAsync(new Exception("Concurrency error"));

            var result = await CreateHandler(examRepo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Duplicate", new TestCaseDetail
            {
                FunctionGroup     = "DuplicateExamTemplate",
                TestCaseID        = "TC-EXMT-DUP-06",
                Description       = "AddAsync throws → catch returns ServerError",
                ExpectedResult    = "Return Failure(ServerError)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception) => Failure(AppErrors.ServerError)" }
            });
        }
    }
}
