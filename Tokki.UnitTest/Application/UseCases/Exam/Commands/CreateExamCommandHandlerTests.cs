using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Commands
{
    public class CreateExamCommandHandlerTests
    {
        private readonly Mock<IExamRepository> _mockExamRepo;
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly Mock<ITemplatePartRepository> _mockPartRepo;
        private readonly Mock<IQuestionBankRepository> _mockBankRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly Mock<ILogger<CreateExamCommandHandler>> _mockLogger;
        private readonly CreateExamCommandHandler _handler;

        public CreateExamCommandHandlerTests()
        {
            _mockExamRepo = new Mock<IExamRepository>();
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockPartRepo = new Mock<ITemplatePartRepository>();
            _mockBankRepo = new Mock<IQuestionBankRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            _handler = new CreateExamCommandHandler(
                _mockExamRepo.Object,
                _mockTemplateRepo.Object,
                _mockPartRepo.Object,
                _mockBankRepo.Object,
                _mockIdGen.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_TitleExists_ReturnsFailure400()
        {
            var command = new CreateExamCommand { Title = "Test Title", ExamTemplateId = "T1" };

            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("Test Title", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_01",
                Description       = "Title already exists",
                ExpectedResult    = "Returns 400 failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Title Exists Boolean" }
            });
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ReturnsFailure404()
        {
            var command = new CreateExamCommand { Title = "A", ExamTemplateId = "T1" };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamTemplate?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_02",
                Description       = "Template id not found",
                ExpectedResult    = "Returns 404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template null" }
            });
        }

        [Fact]
        public async Task Handle_TemplateInactive_ReturnsFailure400()
        {
            var command = new CreateExamCommand { Title = "A", ExamTemplateId = "T1" };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new ExamTemplate { Status = ExamTemplateStatus.Draft });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_03",
                Description       = "Template is not published",
                ExpectedResult    = "Returns 400 failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Draft Status" }
            });
        }

        [Fact]
        public async Task Handle_TemplatePartsEmpty_ReturnsFailure400()
        {
            var command = new CreateExamCommand { Title = "A", ExamTemplateId = "T1" };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new ExamTemplate { ExamTemplateId = "T1", Status = ExamTemplateStatus.Published });
            _mockPartRepo.Setup(x => x.GetByExamTemplateIdAsync("T1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<TemplatePart>());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_04",
                Description       = "Parts list is empty",
                ExpectedResult    = "Returns 400 failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty Parts" }
            });
        }

        [Fact]
        public async Task Handle_MissingDurations_ReturnsFailure400()
        {
            var command = new CreateExamCommand { Title = "A", ExamTemplateId = "T1", SkillDurations = new Dictionary<string, int>() };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new ExamTemplate { ExamTemplateId = "T1", Status = ExamTemplateStatus.Published });
            
            var parts = new List<TemplatePart>
            {
                new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 5 }
            };
            _mockPartRepo.Setup(x => x.GetByExamTemplateIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_05",
                Description       = "Skill durations input is missing",
                ExpectedResult    = "Returns 400 failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SkillDurations missing" }
            });
        }

        [Fact]
        public async Task Handle_NotEnoughQuestions_ReturnsFailure400()
        {
            var command = new CreateExamCommand 
            { 
                Title = "A", 
                ExamTemplateId = "T1", 
                SkillDurations = new Dictionary<string, int> { { "Reading", 60 } } 
            };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new ExamTemplate { ExamTemplateId = "T1", Status = ExamTemplateStatus.Published });
            
            var parts = new List<TemplatePart>
            {
                new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 5, QuestionTypeId = "Type1" }
            };
            _mockPartRepo.Setup(x => x.GetByExamTemplateIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);
            _mockExamRepo.Setup(x => x.GetRecentQuestionIdsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());
            
            // Return 0 questions available
            _mockBankRepo.Setup(x => x.GetByQuestionTypeIdAsync("Type1", QuestionBankStatus.Active, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<QuestionBank>());

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_06",
                Description       = "Not enough pool questions",
                ExpectedResult    = "Returns 400 failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Missing questions count" }
            });
        }

        [Fact]
        public async Task Handle_Success_Returns201()
        {
            var command = new CreateExamCommand 
            { 
                Title = "A", 
                ExamTemplateId = "T1", 
                SkillDurations = new Dictionary<string, int> { { "Reading", 60 } } 
            };
            _mockExamRepo.Setup(x => x.IsTitleExistsAsync("A", It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockTemplateRepo.Setup(x => x.GetByIdAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new ExamTemplate { ExamTemplateId = "T1", Status = ExamTemplateStatus.Published });
            
            var parts = new List<TemplatePart>
            {
                new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 3, QuestionTypeId = "Type1", Mark = 2 }
            };
            _mockPartRepo.Setup(x => x.GetByExamTemplateIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);
            _mockExamRepo.Setup(x => x.GetRecentQuestionIdsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());
            
            var availableQuestions = new List<QuestionBank>
            {
                new QuestionBank { QuestionBankId = "Q1" },
                new QuestionBank { QuestionBankId = "Q2" },
                new QuestionBank { QuestionBankId = "Q3" }
            };
            _mockBankRepo.Setup(x => x.GetByQuestionTypeIdAsync("Type1", QuestionBankStatus.Active, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(availableQuestions);

            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("ID1");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("ID1");

            QACollector.LogTestCase("Exam - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamCommandHandler",
                TestCaseID        = "CreateExamCommandHandler_07",
                Description       = "Creates exam properly",
                ExpectedResult    = "Returns 201 Success",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exact pool question matches" }
            });
        }
    }
}
