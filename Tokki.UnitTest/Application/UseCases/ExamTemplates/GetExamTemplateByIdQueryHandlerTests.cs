using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class GetExamTemplateByIdQueryHandlerTests
    {
        private static GetExamTemplateByIdQueryHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new GetExamTemplateByIdQueryHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static GetExamTemplateByIdQuery ValidQuery => new() { ExamTemplateId = "EXMT-001" };

        private static ExamTemplate BuildTemplateWithParts(ExamTemplateStatus status = ExamTemplateStatus.Published) => new()
        {
            ExamTemplateId = "EXMT-001",
            Name           = "TOEIC Template",
            Description    = "For TOEIC test",
            Type           = ExamType.TopikI,
            Status         = status,
            CreatedAt      = DateTime.UtcNow,
            TemplateParts  = new List<TemplatePart>
            {
                new()
                {
                    TemplatePartId = "PT-001",
                    Skill          = QuestionSkill.Listening,
                    QuestionFrom   = 1,
                    QuestionTo     = 10,
                    Mark           = 5,
                    PartTitle      = "Part 1",
                    Instruction    = "Listen and answer",
                    QuestionTypeId = "QT-001",
                    QuestionType   = new QuestionType { Name = "Multiple Choice" }
                }
            }
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnExamTemplateNotFound()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_01",
                Description       = "Template not found → ExamTemplateNotFound",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "et == null" }
            });
        }

        [Fact]
        public async Task Handle_ValidTemplate_ShouldReturnExamTemplateDto()
        {
            var template = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.ExamTemplateId.Should().Be("EXMT-001");
            result.Data.Name.Should().Be("TOEIC Template");
            result.Data.Status.Should().Be(ExamTemplateStatus.Published);

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_02",
                Description       = "Template found → mapped to ExamTemplateDto correctly",
                ExpectedResult    = "Return Success, Data.ExamTemplateId = 'EXMT-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "et != null => map to ExamTemplateDto" }
            });
        }

        [Fact]
        public async Task Handle_ValidTemplate_ShouldCalculateTotalPartsAndQuestions()
        {
            var template = BuildTemplateWithParts(); // 1 part, Q1-10 = 10 questions
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.Data!.TotalParts.Should().Be(1);
            result.Data.TotalQuestions.Should().Be(10); // Q1..Q10

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_03",
                Description       = "1 part (Q1-Q10) → TotalParts=1, TotalQuestions=10",
                ExpectedResult    = "TotalParts=1, TotalQuestions=10",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Sum(QuestionTo - QuestionFrom + 1) = 10" }
            });
        }

        [Fact]
        public async Task Handle_ValidTemplate_ShouldMapPartsWithQuestionTypeName()
        {
            var template = BuildTemplateWithParts();
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.Data!.Parts.Should().HaveCount(1);
            result.Data!.Parts[0].Skill.Should().Be(QuestionSkill.Listening);
            result.Data.Parts[0].QuestionTypeName.Should().Be("Multiple Choice");
            result.Data.Parts[0].TemplatePartId.Should().Be("PT-001");

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_04",
                Description       = "Parts mapped with Skill=Listening, QuestionTypeName='Multiple Choice'",
                ExpectedResult    = "Parts[0].Skill=Listening, QuestionTypeName='Multiple Choice'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tp.QuestionType != null => QuestionTypeName = tp.QuestionType.Name" }
            });
        }

        [Fact]
        public async Task Handle_PartWithNoQuestionType_ShouldReturnEmptyQuestionTypeName()
        {
            var template = new ExamTemplate
            {
                ExamTemplateId = "EXMT-001",
                Name           = "Test",
                Status         = ExamTemplateStatus.Draft,
                Type           = ExamType.TopikI,
                CreatedAt      = DateTime.UtcNow,
                TemplateParts  = new List<TemplatePart>
                {
                    new()
                    {
                        TemplatePartId = "PT-002",
                        Skill          = QuestionSkill.Reading,
                        QuestionFrom   = 1,
                        QuestionTo     = 5,
                        QuestionTypeId = null,
                        QuestionType   = null
                    }
                }
            };

            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.Data!.Parts[0].QuestionTypeName.Should().Be(string.Empty);
            result.Data.Parts[0].QuestionTypeId.Should().Be(string.Empty);

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_05",
                Description       = "Part has null QuestionType → QuestionTypeName = '' (empty)",
                ExpectedResult    = "Parts[0].QuestionTypeName = ''",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "tp.QuestionType == null => QuestionTypeName = string.Empty" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdWithPartsAsync("EXMT-001", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetExamTemplateById",
                TestCaseID        = "GetExamTemplateById_06",
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
