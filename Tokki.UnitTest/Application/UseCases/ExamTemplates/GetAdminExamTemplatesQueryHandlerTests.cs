using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class GetAdminExamTemplatesQueryHandlerTests
    {
        private static GetAdminExamTemplatesQueryHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null)
        {
            return new GetAdminExamTemplatesQueryHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static GetAdminExamTemplatesQuery DefaultQuery => new()
        {
            PageNumber = 1,
            PageSize   = 10
        };

        private static ExamTemplate BuildTemplate(string id, ExamTemplateStatus status = ExamTemplateStatus.Published) => new()
        {
            ExamTemplateId = id,
            Name           = $"Template {id}",
            Status         = status,
            Type           = ExamType.TopikI,
            CreatedAt      = DateTime.UtcNow,
            TemplateParts  = new List<TemplatePart>
            {
                new() { QuestionFrom = 1, QuestionTo = 10, Mark = 5 }
            }
        };

        [Fact]
        public async Task Handle_EmptyRepository_ShouldReturnEmptyPagedResult()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<ExamTemplateStatus?>(), It.IsAny<CancellationToken>(), It.IsAny<ExamType?>(), It.IsAny<ExamCreatorFilter?>()))
                    .ReturnsAsync((new List<ExamTemplate>(), 0));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_01",
                Description       = "No templates → Return Success, Items empty",
                ExpectedResult    = "Return Success, Items=[]",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "totalCount == 0" }
            });
        }

        [Fact]
        public async Task Handle_MultipleTemplates_ShouldMapToAdminDtos()
        {
            var templates = new List<ExamTemplate>
            {
                BuildTemplate("ET-001"),
                BuildTemplate("ET-002", ExamTemplateStatus.Draft)
            };

            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<ExamTemplateStatus?>(), It.IsAny<CancellationToken>(), It.IsAny<ExamType?>(), It.IsAny<ExamCreatorFilter?>()))
                    .ReturnsAsync((templates, 2));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].ExamTemplateId.Should().Be("ET-001");
            result.Data.Items[1].Status.Should().Be(ExamTemplateStatus.Draft);

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_02",
                Description       = "2 templates → mapped to AdminExamTemplateDto list",
                ExpectedResult    = "Return Success, Items.Count=2, mapped correctly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "items.Select => AdminExamTemplateDto" }
            });
        }

        [Fact]
        public async Task Handle_Template_ShouldCalculateTotalQuestions()
        {
            var tmpl = new ExamTemplate
            {
                ExamTemplateId = "ET-001",
                Name           = "T1",
                Status         = ExamTemplateStatus.Published,
                Type           = ExamType.TopikI,
                CreatedAt      = DateTime.UtcNow,
                TemplateParts  = new List<TemplatePart>
                {
                    new() { QuestionFrom = 1, QuestionTo = 10 },  // 10 questions
                    new() { QuestionFrom = 11, QuestionTo = 15 } //  5 questions
                }
            };

            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<ExamTemplateStatus?>(), It.IsAny<CancellationToken>(), It.IsAny<ExamType?>(), It.IsAny<ExamCreatorFilter?>()))
                    .ReturnsAsync((new List<ExamTemplate> { tmpl }, 1));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.Data!.Items[0].TotalParts.Should().Be(2);
            result.Data.Items[0].TotalQuestions.Should().Be(15); // (10-1+1) + (15-11+1)

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_03",
                Description       = "2 parts (Q1-10, Q11-15) → TotalParts=2, TotalQuestions=15",
                ExpectedResult    = "TotalParts=2, TotalQuestions=15",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Sum(QuestionTo - QuestionFrom + 1) = 15" }
            });
        }

        [Fact]
        public async Task Handle_PaginationQuery_ShouldReturnCorrectMetadata()
        {
            var fiveItems = new List<ExamTemplate>
            {
                BuildTemplate("ET-001"), BuildTemplate("ET-002"), BuildTemplate("ET-003"),
                BuildTemplate("ET-004"), BuildTemplate("ET-005")
            };

            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(2, 5, null, null, It.IsAny<CancellationToken>(), null, It.IsAny<ExamCreatorFilter?>()))
                    .ReturnsAsync((fiveItems, 20));

            var query = new GetAdminExamTemplatesQuery { PageNumber = 2, PageSize = 5 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(20);
            result.Data.TotalPages.Should().Be(4);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_04",
                Description       = "Page 2/5, total=20 → TotalPages=4",
                ExpectedResult    = "TotalPages=4, PageNumber=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PagedResult.Create(..., 20, 2, 5)" }
            });
        }

        [Fact]
        public async Task Handle_StatusFilter_ShouldBePassedToRepository()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    1, 10, null, ExamTemplateStatus.Published, It.IsAny<CancellationToken>(), null, It.IsAny<ExamCreatorFilter?>()))
                    .ReturnsAsync((new List<ExamTemplate> { BuildTemplate("ET-001") }, 1));

            var query = new GetAdminExamTemplatesQuery
            {
                PageNumber = 1,
                PageSize   = 10,
                Status     = ExamTemplateStatus.Published
            };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.GetPagedAsync(
                1, 10, null, ExamTemplateStatus.Published, It.IsAny<CancellationToken>(), null, It.IsAny<ExamCreatorFilter?>()), Times.Once);

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_05",
                Description       = "Status=Published passed directly to repository GetPagedAsync",
                ExpectedResult    = "Return Success, repo called with Status=Published",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status passed to GetPagedAsync" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<ExamTemplateStatus?>(), It.IsAny<CancellationToken>(), It.IsAny<ExamType?>(), It.IsAny<ExamCreatorFilter?>()))
                    .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Get Admin List", new TestCaseDetail
            {
                FunctionGroup     = "GetAdminExamTemplates",
                TestCaseID        = "GetAdminExamTemplates_06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync throws" }
            });
        }
    }
}
