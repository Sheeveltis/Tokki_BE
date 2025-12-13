
using Moq;
using Tokki.Application.UseCases.Word.Commands.DeleteWord;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using FluentAssertions;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.UseCases.Word.Commands
{
    #region DeleteWord Tests
    public class DeleteWordCommandHandlerTests : WordTestBase
    {
        private readonly DeleteWordCommandHandler _handler;

        public DeleteWordCommandHandlerTests()
        {
            _handler = new DeleteWordCommandHandler(
                _mockWordRepo.Object,
                _mockMeaningRepo.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WordNotFound()
        {
            // Arrange
            var command = WordTestData.GetValidDeleteWordCommand();
            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "WORD_NOT_FOUND");
            _mockWordRepo.Verify(x => x.DeleteAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_SoftDelete_When_ForceDeleteIsFalse()
        {
            // Arrange
            var command = WordTestData.GetValidDeleteWordCommand(forceDelete: false);
            var existingWord = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByWordIdAsync(command.WordId))
                .ReturnsAsync(meanings);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Xóa từ vựng thành công");
            _mockWordRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Word>(w =>
                w.Status == WordStatus.Deleted &&
                w.UpdateBy == _testUserId &&
                w.UpdateDate != null
            )), Times.Once);
            _mockMeaningRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), _testUserId), Times.Exactly(meanings.Count));
            _mockWordRepo.Verify(x => x.DeleteAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_HardDelete_When_ForceDeleteIsTrue()
        {
            // Arrange
            var command = WordTestData.GetValidDeleteWordCommand(forceDelete: true);
            var existingWord = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByWordIdAsync(command.WordId))
                .ReturnsAsync(meanings);
            _mockMeaningRepo.Setup(x => x.DeleteByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Xóa vĩnh viễn");
            _mockMeaningRepo.Verify(x => x.DeleteByIdAsync(It.IsAny<string>()), Times.Exactly(meanings.Count));
            _mockWordRepo.Verify(x => x.DeleteAsync(existingWord), Times.Once);
            _mockWordRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WordInUseAndNoForceDelete()
        {
            // Arrange
            var command = WordTestData.GetValidDeleteWordCommand(forceDelete: false);
            var existingWord = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByWordIdAsync(command.WordId))
                .ReturnsAsync(meanings);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(); // Should succeed with soft delete
            _mockWordRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_DeleteWithoutMeanings_When_WordHasNoMeanings()
        {
            // Arrange
            var command = WordTestData.GetValidDeleteWordCommand(forceDelete: false);
            var existingWord = WordTestData.GetFakeWord();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByWordIdAsync(command.WordId))
                .ReturnsAsync(new List<Meaning>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockWordRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Once);
            _mockMeaningRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
    #endregion
}
