using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Queries
{
    public class GetQuestionBanksByQuestionTypeIdQueryHandlerTests : QuestionBankTestBase
    {
        private readonly GetQuestionBanksByQuestionTypeIdQueryHandler _handler;

        public GetQuestionBanksByQuestionTypeIdQueryHandlerTests()
        {
            _handler = new GetQuestionBanksByQuestionTypeIdQueryHandler(_mockQuestionBankRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_MapDtos_And_OrderOptions()
        {
            // Arrange
            var query = QuestionBankTestData.GetByQuestionTypeIdQuery("qt-01");

            var qb = QuestionBankTestData.BuildQuestionBank(
                id: "qb-01",
                status: QuestionBankStatus.Draft,
                questionTypeId: "qt-01",
                passageId: "p-01",
                passage: QuestionBankTestData.BuildPassage("p-01"),
                questionType: QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true, "Reading"),
                options: new List<QuestionOption>
                {
                    QuestionBankTestData.BuildOption("o2", "qb-01", "2", "B", false),
                    QuestionBankTestData.BuildOption("o1", "qb-01", "1", "A", true),
                });

            _mockQuestionBankRepo
                .Setup(x => x.GetByQuestionTypeIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(1);

            var dto = result.Data[0];
            dto.QuestionBankId.Should().Be("qb-01");
            dto.PassageTitle.Should().Be("Passage Title");
            dto.QuestionTypeName.Should().Be("Reading");

            dto.Options.Should().HaveCount(2);
            dto.Options[0].KeyOption.Should().Be("1");
            dto.Options[1].KeyOption.Should().Be("2");

            result.Message.Should().Be("Tìm thấy 1 câu hỏi.");
        }
    }
}
