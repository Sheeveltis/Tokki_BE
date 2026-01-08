using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.ExamTemplates.Queries
{
    public class GetExamTemplateByIdHandlerTests : ExamTemplateTestBase
    {
        private readonly GetExamTemplateByIdQueryHandler _handler;

        public GetExamTemplateByIdHandlerTests()
        {
            _handler = new GetExamTemplateByIdQueryHandler(_mockExamTemplateRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnDto_When_Found()
        {
            var templateId = "TEMPLATE_01";
            var template = ExamTemplateTestData.GetDraftTemplate(templateId);

            var qType = new QuestionType { QuestionTypeId = "QT01", Name = "MCQ" };
            var part = new TemplatePart
            {
                TemplatePartId = "P1",
                QuestionFrom = 1,
                QuestionTo = 10,
                QuestionTypeId = "QT01",
                QuestionType = qType
            };
            template.TemplateParts.Add(part);

            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(templateId, CancellationToken.None))
                                 .ReturnsAsync(template);

            var query = new GetExamTemplateByIdQuery { ExamTemplateId = templateId };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            result.Data.ExamTemplateId.Should().Be(templateId);
            result.Data.Name.Should().Be(template.Name);

            result.Data.Parts.Should().HaveCount(1);
            var partDto = result.Data.Parts.First(); 
            partDto.TemplatePartId.Should().Be("P1");
            partDto.QuestionTypeName.Should().Be("MCQ");

            result.Data.TotalParts.Should().Be(1);
            result.Data.TotalQuestions.Should().Be(10); 
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NotFound()
        {
            var templateId = "NON_EXISTENT";
            _mockExamTemplateRepo.Setup(x => x.GetByIdWithPartsAsync(templateId, CancellationToken.None))
                                 .ReturnsAsync((ExamTemplate?)null);

            var query = new GetExamTemplateByIdQuery { ExamTemplateId = templateId };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(AppErrors.ExamTemplateNotFound);
        }
    }
}