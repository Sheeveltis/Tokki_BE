using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;

namespace Tokki.UnitTests.Features.ExamTemplates.Commands
{
    public class DuplicateExamTemplateHandlerTests : ExamTemplateTestBase
    {
        private readonly DuplicateExamTemplateCommandHandler _handler;

        public DuplicateExamTemplateHandlerTests()
        {
            var logger = new Mock<ILogger<DuplicateExamTemplateCommandHandler>>();
            _handler = new DuplicateExamTemplateCommandHandler(
                _mockExamTemplateRepo.Object,
                _mockTemplatePartRepo.Object,
                _mockIdGenerator.Object,
                logger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TemplateExists()
        {
            var original = ExamTemplateTestData.GetDraftTemplate();
            original.TemplateParts.Add(new TemplatePart { PartTitle = "Part 1", QuestionFrom = 1, QuestionTo = 10 });
            
            var command = new DuplicateExamTemplateCommand(original.ExamTemplateId);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(original.ExamTemplateId, CancellationToken.None))
                                 .ReturnsAsync(original);
            
            _mockExamTemplateRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null))
                                 .ReturnsAsync(false);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("NEW_ID");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("NEW_ID");

            _mockExamTemplateRepo.Verify(x => x.AddAsync(It.Is<ExamTemplate>(e => e.ExamTemplateId == "NEW_ID")), Times.Once);
            _mockTemplatePartRepo.Verify(x => x.AddRangeAsync(It.Is<List<TemplatePart>>(l => l.Count == 1)), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(It.IsAny<string>(), CancellationToken.None))
                                 .ReturnsAsync((ExamTemplate?)null);

            var result = await _handler.Handle(new DuplicateExamTemplateCommand("INVALID_ID"), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(AppErrors.ExamTemplateNotFound);
        }
    }
}