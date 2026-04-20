using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetQuestionsByPartQueryHandlerTests
    {
        private static GetQuestionsByPartQueryHandler CreateHandler(
            Mock<ITemplatePartRepository>? partRepo = null,
            Mock<IQuestionBankRepository>? bankRepo = null)
        {
            return new GetQuestionsByPartQueryHandler(
                (partRepo ?? new Mock<ITemplatePartRepository>()).Object,
                (bankRepo ?? new Mock<IQuestionBankRepository>()).Object);
        }

        private static TemplatePart GetSamplePart() => new()
        {
            TemplatePartId = "PART-001",
            QuestionTypeId = "QT-001",
            Skill          = QuestionSkill.Listening
        };

        private static IEnumerable<QuestionBank> GetSampleBanks() => new List<QuestionBank>
        {
            new()
            {
                QuestionBankId  = "QB-001",
                Content         = "Question 1",
                QuestionOptions = new List<QuestionOption> { new() { KeyOption = "1", IsCorrect = true, Content = "A" } },
                QuestionType    = new() { Skill = QuestionSkill.Listening }
            }
        };

        // Get_Questions_By_Part_01 | A | TemplatePart not found → 404
        [Fact]
        public async Task Handle_TemplatePartNotFound_ShouldReturn404()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            mockPart.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((TemplatePart?)null);

            var query = new GetQuestionsByPartQuery { TemplatePartId = "GHOST" };
            var result = await CreateHandler(mockPart).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.TemplatePartNotFound);

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_01",
                Description = "TemplatePart ID not found in repository",
                ExpectedResult = "Return 404 TemplatePartNotFound", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // Get_Questions_By_Part_02 | N | Valid part → returns questions
        [Fact]
        public async Task Handle_ValidPart_ShouldReturnQuestions()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart());
            mockBank.Setup(x => x.GetAvailableQuestionsByTypeAsync("QT-001", 1, 10, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GetSampleBanks(), 1));

            var query = new GetQuestionsByPartQuery { TemplatePartId = "PART-001", PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mockPart, mockBank).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_02",
                Description = "Valid TemplatePartId returns available questions",
                ExpectedResult = "Return 200 with questions", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplatePart found, questions returned" }
            });
        }

        // Get_Questions_By_Part_03 | N | DTO mapping — Listening → media type = Audio
        [Fact]
        public async Task Handle_ListeningPart_ShouldMapMediaTypeToAudio()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart());
            mockBank.Setup(x => x.GetAvailableQuestionsByTypeAsync("QT-001", 1, 10, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GetSampleBanks(), 1));

            var result = await CreateHandler(mockPart, mockBank).Handle(
                new GetQuestionsByPartQuery { TemplatePartId = "PART-001", PageNumber = 1, PageSize = 10 },
                CancellationToken.None);

            result.Data!.Items.First().MediaType.Should().Be("Audio");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_03",
                Description = "Listening questions should have MediaType = Audio",
                ExpectedResult = "MediaType = 'Audio' for Listening skill", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "MapSkillToMediaType(Listening) = Audio" }
            });
        }

        // Get_Questions_By_Part_04 | N | Reading/Writing → media type = Image
        [Fact]
        public async Task Handle_ReadingPart_ShouldMapMediaTypeToImage()
        {
            var readingPart = new TemplatePart { TemplatePartId = "PART-002", QuestionTypeId = "QT-002", Skill = QuestionSkill.Reading };
            var readingBank = new List<QuestionBank>
            {
                new() { QuestionBankId = "QB-002", Content = "Q2", QuestionOptions = new List<QuestionOption>(),
                        QuestionType = new() { Skill = QuestionSkill.Reading } }
            };

            var mockPart = new Mock<ITemplatePartRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-002", It.IsAny<CancellationToken>())).ReturnsAsync(readingPart);
            mockBank.Setup(x => x.GetAvailableQuestionsByTypeAsync("QT-002", 1, 10, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((readingBank, 1));

            var result = await CreateHandler(mockPart, mockBank).Handle(
                new GetQuestionsByPartQuery { TemplatePartId = "PART-002", PageNumber = 1, PageSize = 10 },
                CancellationToken.None);

            result.Data!.Items.First().MediaType.Should().Be("Image");

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_04",
                Description = "Reading questions should have MediaType = Image",
                ExpectedResult = "MediaType = 'Image'", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "MapSkillToMediaType(Reading) = Image" }
            });
        }

        // Get_Questions_By_Part_05 | N | Empty question bank returns 200 with empty items
        [Fact]
        public async Task Handle_EmptyBank_ShouldReturn200WithEmpty()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart());
            mockBank.Setup(x => x.GetAvailableQuestionsByTypeAsync("QT-001", 1, 10, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((new List<QuestionBank>(), 0));

            var result = await CreateHandler(mockPart, mockBank).Handle(
                new GetQuestionsByPartQuery { TemplatePartId = "PART-001", PageNumber = 1, PageSize = 10 },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_05",
                Description = "No available questions for this part type → empty list returned",
                ExpectedResult = "200 with empty Items", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty bank returns 200 not 404" }
            });
        }

        // Get_Questions_By_Part_06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_BankRepositoryThrows_ShouldPropagateException()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart());
            mockBank.Setup(x => x.GetAvailableQuestionsByTypeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Down"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockPart, mockBank).Handle(
                    new GetQuestionsByPartQuery { TemplatePartId = "PART-001", PageNumber = 1, PageSize = 10 },
                    CancellationToken.None));

            QACollector.LogTestCase("Exam - Get Questions By Part", new TestCaseDetail
            {
                FunctionGroup = "Get Questions By Part", TestCaseID = "Get_Questions_By_Part_06",
                Description = "QuestionBank repository throws exception",
                ExpectedResult = "Exception propagates", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync" }
            });
        }
    }
}
