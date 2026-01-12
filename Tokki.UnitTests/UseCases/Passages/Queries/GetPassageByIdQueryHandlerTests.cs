using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.Queries.GetPassageById;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Passages.Queries
{
    public class GetPassageByIdQueryHandlerTests : PassageTestBase
    {
        private readonly GetPassageByIdQueryHandler _handler;

        public GetPassageByIdQueryHandlerTests()
        {
            _handler = new GetPassageByIdQueryHandler(_mockPassageRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_PassageNotExists()
        {
            // Arrange
            var query = PassageTestData.GetByIdQuery("  pass-01  ");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tokki.Domain.Entities.Passage?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_MapDto_When_Found()
        {
            // Arrange
            var query = PassageTestData.GetByIdQuery("pass-01");

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Title",
                mediaType: PassageMediaType.Audio,
                status: PassageStatus.Active,
                audioUrl: "a.mp3",
                createdAt: DateTime.UtcNow);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();

            result.Data.PassageId.Should().Be("pass-01");
            result.Data.Title.Should().Be("Title");
            result.Data.AudioUrl.Should().Be("a.mp3");
            result.Data.MediaType.Should().Be(PassageMediaType.Audio);
            result.Data.Status.Should().Be(PassageStatus.Active);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_ExceptionThrown()
        {
            // Arrange
            var query = PassageTestData.GetByIdQuery("pass-01");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ServerError.Code);
        }
    }
}
