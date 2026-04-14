using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateTemplatePart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class UpdateExamTemplatePartCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly Mock<IQuestionTypeRepository> _mockTypeRepo;
        private readonly UpdateExamTemplatePartCommandHandler _handler;

        public UpdateExamTemplatePartCommandHandlerTests()
        {
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockTypeRepo = new Mock<IQuestionTypeRepository>();
            _handler = new UpdateExamTemplatePartCommandHandler(_mockTemplateRepo.Object, _mockTypeRepo.Object);
        }

        // TC-EXT-UTP-01 | A | Template Not Found -> Error
        [Fact]
        public async Task Handle_TemplateNotFound_ShouldFail()
        {
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ExamTemplate)null);

            var command = new UpdateExamTemplatePartCommand { ExamTemplateId = "T1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy đề thi mẫu.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-01",
                Description = "Returns error if template fails object mapping natively",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template DB null" }
            });
        }

        // TC-EXT-UTP-02 | A | Status Not Draft -> Error
        [Fact]
        public async Task Handle_StatusNotDraft_ShouldFail()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Published };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand { ExamTemplateId = "T1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("trạng thái Nháp.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-02",
                Description = "Blocks editing sections when exam configuration commits beyond draft stage",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != Draft" }
            });
        }

        // TC-EXT-UTP-03 | A | PartId Not Found -> Error
        [Fact]
        public async Task Handle_PartIdNotFound_ShouldFail()
        {
            var template = new ExamTemplate 
            { 
                Status = ExamTemplateStatus.Draft,
                TemplateParts = new List<TemplatePart> { new TemplatePart { TemplatePartId = "PT_AAA" } }
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand { ExamTemplateId = "T1", TemplatePartId = "PT_NOTFOUND" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy phần thi này trong đề thi.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-03",
                Description = "Safeguards against orphan edits where component arrays reject bad injection attempts",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PartId not in list" }
            });
        }

        // TC-EXT-UTP-04 | A | Overlap Detect With Other Parts
        [Fact]
        public async Task Handle_OverlapWithOther_ShouldFail()
        {
            var template = new ExamTemplate 
            { 
                Status = ExamTemplateStatus.Draft,
                TemplateParts = new List<TemplatePart> 
                { 
                    new TemplatePart { TemplatePartId = "P1", QuestionFrom = 1, QuestionTo = 10 },
                    new TemplatePart { TemplatePartId = "P2", QuestionFrom = 11, QuestionTo = 20 }
                }
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            // Tries updating P2 to start from 10, overlapping with P1
            var command = new UpdateExamTemplatePartCommand { ExamTemplateId = "T1", TemplatePartId = "P2", QuestionFrom = 10, QuestionTo = 15 };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Phạm vi câu hỏi bị trùng lặp.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-04",
                Description = "Enforces non-overlapping array slots excluding its own identity reference securely",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Overlap element 10 with P1" }
            });
        }

        // TC-EXT-UTP-05 | A | QuestionType Match Skill Failure
        [Fact]
        public async Task Handle_SkillMismatchUpdate_ShouldFail()
        {
            var template = new ExamTemplate 
            { 
                Status = ExamTemplateStatus.Draft,
                TemplateParts = new List<TemplatePart> 
                { 
                    new TemplatePart { TemplatePartId = "P1", QuestionFrom = 1, QuestionTo = 10, Skill = QuestionSkill.Listening }
                }
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);
            
            _mockTypeRepo.Setup(x => x.GetByIdAsync("QT1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { Skill = QuestionSkill.Reading });

            var command = new UpdateExamTemplatePartCommand { ExamTemplateId = "T1", TemplatePartId = "P1", QuestionTypeId = "QT1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Kỹ năng không khớp.");

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-05",
                Description = "Rejects edits placing reading question types into listening configurations safely",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Update attempts mismatched skill" }
            });
        }

        // TC-EXT-UTP-06 | N | Success Path Partial Update
        [Fact]
        public async Task Handle_ValidPartialUpdate_ShouldSucceed()
        {
            var part = new TemplatePart { TemplatePartId = "P1", PartTitle = "Old", QuestionFrom = 1, QuestionTo = 10 };
            var template = new ExamTemplate 
            { 
                Status = ExamTemplateStatus.Draft,
                TemplateParts = new List<TemplatePart> { part }
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand 
            { 
                ExamTemplateId = "T1", 
                TemplatePartId = "P1", 
                Instruction = "Read well",
                Mark = 2 
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            part.Instruction.Should().Be("Read well");
            part.Mark.Should().Be(2);
            _mockTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);

            QACollector.LogTestCase("Exam Template - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateExamTemplatePartCommandHandler",
                TestCaseID = "TC-EXT-UTP-06",
                Description = "Successfully modifies specified isolated values ignoring unsupplied fallback properties unconditionally",
                ExpectedResult = "Success true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Partial Update Instructions/Mark" }
            });
        }
    }
}
