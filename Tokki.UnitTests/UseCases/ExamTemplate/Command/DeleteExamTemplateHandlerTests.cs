using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.UseCases.ExamTemplates.Commands.DeleteExamTemplate;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class DeleteExamTemplateHandlerTests : ExamTemplateTestBase
    {
        private readonly DeleteExamTemplateCommandHandler _handler;

        public DeleteExamTemplateHandlerTests()
        {
            var logger = new Mock<ILogger<DeleteExamTemplateCommandHandler>>();
            _handler = new DeleteExamTemplateCommandHandler(_mockExamTemplateRepo.Object, logger.Object);
        }

        [Fact]
        public async Task Handle_Should_SoftDelete_When_Found()
        {
            var template = ExamTemplateTestData.GetDraftTemplate();
            var command = new DeleteExamTemplateCommand(template.ExamTemplateId);

            _mockExamTemplateRepo.Setup(x => x.GetByIdAsync(template.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(ExamTemplateStatus.Deleted);

            _mockExamTemplateRepo.Verify(x => x.UpdateAsync(template), Times.Once);
            _mockExamTemplateRepo.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        }
    }
}