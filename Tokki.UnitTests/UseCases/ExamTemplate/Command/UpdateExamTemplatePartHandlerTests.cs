using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateTemplatePart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class UpdateExamTemplatePartHandlerTests : ExamTemplateTestBase
    {
        private readonly UpdateExamTemplatePartCommandHandler _handler;

        public UpdateExamTemplatePartHandlerTests()
        {
            _handler = new UpdateExamTemplatePartCommandHandler(
                _mockExamTemplateRepo.Object,
                _mockQuestionTypeRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UpdateIsValid()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();

            var existingPart = new TemplatePart
            {
                TemplatePartId = "PART_01",
                ExamTemplateId = template.ExamTemplateId,
                QuestionFrom = 1,
                QuestionTo = 10,
                Skill = QuestionSkill.Reading,
                QuestionTypeId = "QT_OLD"
            };
            template.TemplateParts.Add(existingPart);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand
            {
                ExamTemplateId = template.ExamTemplateId,
                TemplatePartId = "PART_01",
                QuestionFrom = 1,
                QuestionTo = 15, 
                PartTitle = "Updated Title"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            var updatedPart = template.TemplateParts.First(p => p.TemplatePartId == "PART_01");
            updatedPart.QuestionTo.Should().Be(15);
            updatedPart.PartTitle.Should().Be("Updated Title");

            _mockExamTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);
            _mockExamTemplateRepo.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PartNotFound()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand
            {
                ExamTemplateId = template.ExamTemplateId,
                TemplatePartId = "NON_EXISTENT_ID" 
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy phần thi này");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RangeOverlaps()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            template.TemplateParts.Add(new TemplatePart { TemplatePartId = "PART_1", QuestionFrom = 1, QuestionTo = 10 });
            template.TemplateParts.Add(new TemplatePart { TemplatePartId = "PART_2", QuestionFrom = 20, QuestionTo = 30 });

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var command = new UpdateExamTemplatePartCommand
            {
                ExamTemplateId = template.ExamTemplateId,
                TemplatePartId = "PART_1",
                QuestionFrom = 15,
                QuestionTo = 25
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("trùng lặp");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_QuestionTypeMismatchSkill()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            var part = new TemplatePart
            {
                TemplatePartId = "PART_1",
                Skill = QuestionSkill.Reading, 
                QuestionFrom = 1,
                QuestionTo = 10
            };
            template.TemplateParts.Add(part);

            var newQType = new QuestionType { QuestionTypeId = "QT_NEW", Skill = QuestionSkill.Listening };

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            _mockQuestionTypeRepo.Setup(x => x.GetByIdAsync("QT_NEW", CancellationToken.None))
                                 .ReturnsAsync(newQType);

            var command = new UpdateExamTemplatePartCommand
            {
                ExamTemplateId = template.ExamTemplateId,
                TemplatePartId = "PART_1",
                QuestionTypeId = "QT_NEW" 
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Kỹ năng không khớp");
        }
    }
}