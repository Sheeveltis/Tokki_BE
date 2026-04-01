using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.Commands.UpdatePassage;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Passages.Commands
{
    public class UpdatePassageCommandHandlerTests : PassageTestBase
    {
        private readonly UpdatePassageCommandHandler _handler;

        public UpdatePassageCommandHandlerTests()
        {
            _handler = new UpdatePassageCommandHandler(_mockPassageRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_PassageNotExists()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(passageId: "  pass-01  ");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tokki.Domain.Entities.Passage?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_TextMediaType_RequiresContent()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(
                passageId: "pass-01",
                title: null,
                content: "   ", // whitespace => incomingContent null
                mediaType: PassageMediaType.Text);

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Title",
                mediaType: PassageMediaType.Text,
                status: PassageStatus.Active,
                content: null); // content null => fail

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("The Text type must have content.");

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Never);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_ImageMediaType_RequiresImageUrl()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(
                passageId: "pass-01",
                imageUrl: "  ", // whitespace => incoming null
                mediaType: PassageMediaType.Image);

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Title",
                mediaType: PassageMediaType.Image,
                imageUrl: null);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Image type must have an image link.");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_AudioMediaType_RequiresAudioUrl()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(
                passageId: "pass-01",
                audioUrl: "  ", // whitespace => incoming null
                mediaType: PassageMediaType.Audio);

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Title",
                mediaType: PassageMediaType.Audio,
                audioUrl: null);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Audio type must have an audio link.");
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_TitleDuplicated()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(
                passageId: "pass-01",
                title: "  New Title  ",
                mediaType: null);

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Old Title",
                mediaType: PassageMediaType.Text,
                content: "content");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockPassageRepo
                .Setup(x => x.IsTitleExistsAsync("New Title", "pass-01"))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageTitleDuplicated.Code);

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Never);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateToAudio_And_NormalizeFields_When_Valid()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(
                passageId: "pass-01",
                title: "  Title  ", // đổi title (trim)
                audioUrl: " https://audio/a.mp3 ",
                mediaType: PassageMediaType.Audio);

            var passage = PassageTestData.BuildPassage(
                passageId: "pass-01",
                title: "Old",
                mediaType: PassageMediaType.Text,
                content: "old content",
                imageUrl: "old img",
                audioUrl: "old audio",
                status: PassageStatus.Active);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockPassageRepo
                .Setup(x => x.IsTitleExistsAsync("Title", "pass-01"))
                .ReturnsAsync(false);

            _mockPassageRepo
                .Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()))
                .Returns(Task.CompletedTask);

            _mockPassageRepo
     .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
     .ReturnsAsync(true);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("pass-01");
            result.Message.Should().Be("Updated paragraph successfully.");

            passage.Title.Should().Be("Title");
            passage.MediaType.Should().Be(PassageMediaType.Audio);
            passage.AudioUrl.Should().Be("https://audio/a.mp3");
            passage.Content.Should().BeNull();
            passage.ImageUrl.Should().BeNull();

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Once);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_ExceptionThrown()
        {
            // Arrange
            var command = PassageTestData.GetUpdateCommand(passageId: "pass-01");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
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
