using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates.Commands
{
    public class AddTemplatePartsCommandHandlerTests
    {
        private readonly Mock<IExamTemplateRepository> _mockTemplateRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly Mock<IQuestionTypeRepository> _mockTypeRepo;
        private readonly AddTemplatePartsCommandHandler _handler;

        public AddTemplatePartsCommandHandlerTests()
        {
            _mockTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockTypeRepo = new Mock<IQuestionTypeRepository>();
            _handler = new AddTemplatePartsCommandHandler(_mockTemplateRepo.Object, _mockIdGen.Object, _mockTypeRepo.Object);
        }

        // AddTemplatePartsCommandHandler_01 | A | Template Not Found
        [Fact]
        public async Task Handle_TemplateNotFound_ShouldFail()
        {
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync((ExamTemplate)null);

            var command = new AddTemplatePartsCommand { ExamTemplateId = "T1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không tìm thấy đề thi mẫu.");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_01",
                Description = "Returns failure immediately if template record vanishes",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template DB null" }
            });
        }

        // AddTemplatePartsCommandHandler_02 | A | Status Not Draft
        [Fact]
        public async Task Handle_StatusNotDraft_ShouldFail()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Published };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new AddTemplatePartsCommand { ExamTemplateId = "T1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("trạng thái Nháp (Draft).");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_02",
                Description = "Blocks structural alterations natively when outside of Draft contexts",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Published" }
            });
        }

        // AddTemplatePartsCommandHandler_03 | A | Invalid Frame (From > To)
        [Fact]
        public async Task Handle_InvalidRangeFromGtTo_ShouldFail()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new AddTemplatePartsCommand 
            { 
                ExamTemplateId = "T1", 
                Parts = new List<CreateTemplatePartDto> { new CreateTemplatePartDto { QuestionFrom = 5, QuestionTo = 3 } } 
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Phạm vi câu hỏi không hợp lệ:");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_03",
                Description = "Denies inverted mathematical constraints preventing rendering logic crash loops later",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionFrom > QuestionTo" }
            });
        }

        // AddTemplatePartsCommandHandler_04 | A | Overlap Detect
        [Fact]
        public async Task Handle_OverlappingBounds_ShouldFail()
        {
            var template = new ExamTemplate 
            { 
                Status = ExamTemplateStatus.Draft, 
                TemplateParts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 10 } }
            };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);

            var command = new AddTemplatePartsCommand 
            { 
                ExamTemplateId = "T1", 
                Parts = new List<CreateTemplatePartDto> { new CreateTemplatePartDto { QuestionFrom = 10, QuestionTo = 15 } } 
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("bị trùng lặp với các phần thi đã có.");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_04",
                Description = "Ensures disjoint array maps verifying boundaries precisely using Max/Min bounds intersections",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Overlap on element 10" }
            });
        }

        // AddTemplatePartsCommandHandler_05 | A | QuestionType Skill Discord
        [Fact]
        public async Task Handle_MismatchedSkillTypes_ShouldFail()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);
            
            _mockTypeRepo.Setup(x => x.GetByIdAsync("QT1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { Skill = QuestionSkill.Reading });

            var command = new AddTemplatePartsCommand 
            { 
                ExamTemplateId = "T1", 
                Parts = new List<CreateTemplatePartDto> { new CreateTemplatePartDto { QuestionFrom = 1, QuestionTo = 5, QuestionTypeId = "QT1", Skill = QuestionSkill.Listening } } 
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Kỹ năng không khớp.");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_05",
                Description = "Prevents listening configurations forcing assignment into reading containers",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill provided Listening vs Type Reading" }
            });
        }

        // AddTemplatePartsCommandHandler_06 | N | Clean Addition Loop
        [Fact]
        public async Task Handle_ValidFlow_ShouldAddSuccessfully()
        {
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft };
            _mockTemplateRepo.Setup(x => x.GetByIdWithPartsAsync("T1", It.IsAny<CancellationToken>()))
                             .ReturnsAsync(template);
            
            _mockTypeRepo.Setup(x => x.GetByIdAsync("QT1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new QuestionType { Skill = QuestionSkill.Reading });
            
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("ID12345");

            var command = new AddTemplatePartsCommand 
            { 
                ExamTemplateId = "T1", 
                Parts = new List<CreateTemplatePartDto> { new CreateTemplatePartDto { QuestionFrom = 1, QuestionTo = 5, QuestionTypeId = "QT1", Skill = QuestionSkill.Reading } } 
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.TemplateParts.Count.Should().Be(1);
            _mockTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup = "AddTemplatePartsCommandHandler",
                TestCaseID = "AddTemplatePartsCommandHandler_06",
                Description = "Valid configuration appends appropriately parsing constraints gracefully",
                ExpectedResult = "Success true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Clean loop insertion" }
            });
        }
    }
}
