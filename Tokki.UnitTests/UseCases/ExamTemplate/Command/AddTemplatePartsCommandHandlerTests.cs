using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class AddTemplatePartsCommandHandlerTests : ExamTemplateTestBase
    {
        private readonly AddTemplatePartsCommandHandler _handler;

        public AddTemplatePartsCommandHandlerTests()
        {
            _handler = new AddTemplatePartsCommandHandler(
                _mockExamTemplateRepo.Object,
                _mockIdGenerator.Object,
                _mockQuestionTypeRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_PartsValid()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            var command = ExamTemplateTestData.GetAddPartsCommand(template.ExamTemplateId);
            var qType = ExamTemplateTestData.GetQuestionType("QT_01", QuestionSkill.Reading);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            _mockQuestionTypeRepo.Setup(x => x.GetByIdAsync("QT_01", CancellationToken.None))
                                 .ReturnsAsync(qType);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("PART_ID");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.TemplateParts.Should().HaveCount(1);
            template.TemplateParts.First().QuestionFrom.Should().Be(1);

            _mockExamTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OverlapInRange()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            template.TemplateParts.Add(new TemplatePart { QuestionFrom = 1, QuestionTo = 10 });

            var command = ExamTemplateTestData.GetAddPartsCommand(template.ExamTemplateId);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("trùng lặp");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_SkillMismatch()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            var command = ExamTemplateTestData.GetAddPartsCommand(template.ExamTemplateId);
            var qType = ExamTemplateTestData.GetQuestionType("QT_01", QuestionSkill.Listening);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            _mockQuestionTypeRepo.Setup(x => x.GetByIdAsync("QT_01", CancellationToken.None))
                                 .ReturnsAsync(qType);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Kỹ năng không khớp");
        }
    }
}