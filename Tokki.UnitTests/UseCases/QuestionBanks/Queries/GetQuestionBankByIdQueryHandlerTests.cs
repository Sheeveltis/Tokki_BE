using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Queries
{
    public class GetQuestionBankByIdQueryHandlerTests : QuestionBankTestBase
    {
        private readonly GetQuestionBankByIdQueryHandler _handler;

        public GetQuestionBankByIdQueryHandlerTests()
        {
            _handler = new GetQuestionBankByIdQueryHandler(_mockQuestionBankRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_NotExists()
        {
            // Arrange
            var query = QuestionBankTestData.GetByIdQuery("qb-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_OrderOptions()
        {
            // Arrange
            var query = QuestionBankTestData.GetByIdQuery("qb-01");

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
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            result.Data.QuestionBankId.Should().Be("qb-01");
            result.Data.Options[0].KeyOption.Should().Be("1");
            result.Data.Options[1].KeyOption.Should().Be("2");
        }
    }
}
