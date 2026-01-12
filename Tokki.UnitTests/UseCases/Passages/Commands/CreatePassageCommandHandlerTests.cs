using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.Commands.CreatePassage;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Passages.Commands
{
    public class CreatePassageCommandHandlerTests : PassageTestBase
    {
        private readonly CreatePassageCommandHandler _handler;

        public CreatePassageCommandHandlerTests()
        {
            _handler = new CreatePassageCommandHandler(
                _mockPassageRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_TitleDuplicated()
        {
            // Arrange
            var command = PassageTestData.GetCreateCommand(
                title: "  My Title  ",
                content: "abc",
                mediaType: PassageMediaType.Text);

            _mockPassageRepo
      .Setup(x => x.IsTitleExistsAsync("My Title", (string?)null))
      .ReturnsAsync(true);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageTitleDuplicated.Code);

            _mockIdGenerator.Verify(x => x.GenerateCustom(It.IsAny<int>()), Times.Never);
            _mockPassageRepo.Verify(x => x.AddAsync(It.IsAny<Passage>()), Times.Never);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_CreatePassage_And_Return201_When_Valid()
        {
            // Arrange
            var command = PassageTestData.GetCreateCommand(
                title: "  My Title  ",
                content: "content",
                mediaType: PassageMediaType.Text);

            _mockPassageRepo
    .Setup(x => x.IsTitleExistsAsync("My Title", (string?)null))
    .ReturnsAsync(false);


            _mockIdGenerator
                .Setup(x => x.GenerateCustom(10))
                .Returns("pass-99");

            Passage? captured = null;

            _mockPassageRepo
                .Setup(x => x.AddAsync(It.IsAny<Passage>()))
                .Callback<Passage>(p => captured = p)
                .Returns(Task.CompletedTask);

            _mockPassageRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("pass-99");
            result.Message.Should().Be("Tạo đoạn văn thành công.");

            captured.Should().NotBeNull();
            captured!.PassageId.Should().Be("pass-99");
            captured.Title.Should().Be("My Title");
            captured.MediaType.Should().Be(PassageMediaType.Text);
            captured.Status.Should().Be(PassageStatus.Active);
            captured.Content.Should().Be("content");
            captured.ImageUrl.Should().BeNull();
            captured.AudioUrl.Should().BeNull();
            captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(7), TimeSpan.FromSeconds(5));

            _mockPassageRepo.Verify(x => x.AddAsync(It.IsAny<Passage>()), Times.Once);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_ExceptionThrown()
        {
            // Arrange
            var command = PassageTestData.GetCreateCommand(
                title: "Title",
                content: "content",
                mediaType: PassageMediaType.Text);

            _mockPassageRepo
      .Setup(x => x.IsTitleExistsAsync("Title", (string?)null))
      .ReturnsAsync(false);

            _mockIdGenerator
                .Setup(x => x.GenerateCustom(10))
                .Returns("pass-99");

            _mockPassageRepo
                .Setup(x => x.AddAsync(It.IsAny<Passage>()))
                .ThrowsAsync(new Exception("db"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ServerError.Code);
        }
    }
}
