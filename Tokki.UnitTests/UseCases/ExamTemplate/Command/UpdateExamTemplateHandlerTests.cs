using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class UpdateExamTemplateHandlerTests : ExamTemplateTestBase
    {
        private readonly UpdateExamTemplateCommandHandler _handler;

        public UpdateExamTemplateHandlerTests()
        {
            _handler = new UpdateExamTemplateCommandHandler(_mockExamTemplateRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UpdateValidDraft()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            var command = new UpdateExamTemplateCommand
            {
                ExamTemplateId = template.ExamTemplateId,
                Name = "Updated Name"
            };

            _mockExamTemplateRepo.Setup(x => x.GetByIdAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            _mockExamTemplateRepo.Setup(x => x.IsNameExistsAsync(command.Name, template.ExamTemplateId))
                                 .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Name.Should().Be("Updated Name");
            _mockExamTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotDraft()
        {
            var template = ExamTemplateTestData.GetPublishedTemplate();
            var command = new UpdateExamTemplateCommand { ExamTemplateId = template.ExamTemplateId };

            _mockExamTemplateRepo.Setup(x => x.GetByIdAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Draft status");
        }
    }
}