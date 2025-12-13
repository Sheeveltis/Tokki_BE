using Moq;
using Tokki.Application.UseCases.Word.Commands.UpdateWord;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using FluentAssertions;

namespace Tokki.UnitTests.UseCases.Word.Commands
{
    #region UpdateWord Tests
    public class UpdateWordCommandHandlerTests : WordTestBase
    {
        private readonly UpdateWordCommandHandler _handler;

        public UpdateWordCommandHandlerTests()
        {
            _handler = new UpdateWordCommandHandler(
                _mockWordRepo.Object,
                _mockMeaningRepo.Object,
                _mockTtsService.Object,
                _mockCloudinaryService.Object,
                _mockIdGen.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WordNotFound()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "WORD_NOT_FOUND");
            _mockWordRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateWord_When_TextChanged()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            var existingWord = WordTestData.GetFakeWord();
            existingWord.Text = "다른 단어"; // Different text

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockWordRepo.Setup(x => x.GetByTextAsync(command.Text!))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);
            _mockTtsService.Setup(x => x.SynthesizeKoreanAudioAsync(command.Text!))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockCloudinaryService.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.com/new-audio.mp3");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Text.Should().Be(command.Text);
            _mockWordRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Word>(w =>
                w.Text == command.Text &&
                w.UpdateBy == _testUserId &&
                w.UpdateDate != null
            )), Times.Once);
            _mockTtsService.Verify(x => x.SynthesizeKoreanAudioAsync(command.Text!), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TextAlreadyExists()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            var existingWord = WordTestData.GetFakeWord();
            var duplicateWord = new Tokki.Domain.Entities.Word { WordId = "WORD-DIFFERENT", Text = command.Text! };

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockWordRepo.Setup(x => x.GetByTextAsync(command.Text!))
                .ReturnsAsync(duplicateWord);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "WORD_DUPLICATED");
            _mockWordRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdatePronunciation_When_Provided()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            command.Text = null; // Only update pronunciation
            var existingWord = WordTestData.GetFakeWord();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockWordRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Word>(w =>
                w.Pronunciation == command.Pronunciation
            )), Times.Once);
            _mockTtsService.Verify(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateExistingMeanings_When_MeaningIdProvided()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            command.Text = null; // Don't update text
            var existingWord = WordTestData.GetFakeWord();
            var existingMeaning = WordTestData.GetFakeMeanings().First();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByIdAsync(command.Meanings![0].MeaningId!))
                .ReturnsAsync(existingMeaning);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.UpdateAsync(It.Is<Meaning>(m =>
                m.MeaningId == command.Meanings![0].MeaningId &&
                m.Definition == command.Meanings[0].Definition &&
                m.UpdateBy == _testUserId
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_AddNewMeanings_When_MeaningIdNotProvided()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            command.Text = null;
            command.Meanings = new List<MeaningUpdateDto>
            {
                new MeaningUpdateDto
                {
                    MeaningId = null, // New meaning
                    Definition = "Nghĩa mới",
                    ExampleSentence = "Câu ví dụ mới"
                }
            };
            var existingWord = WordTestData.GetFakeWord();

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockIdGen.Setup(x => x.Generate(15))
                .Returns("NEW-MEANING-ID");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.AddAsync(It.Is<Meaning>(m =>
                m.WordId == existingWord.WordId &&
                m.Definition == "Nghĩa mới" &&
                m.CreateBy == _testUserId
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_IgnoreMeaningOfDifferentWord_When_Updating()
        {
            // Arrange
            var command = WordTestData.GetValidUpdateWordCommand();
            command.Text = null;
            var existingWord = WordTestData.GetFakeWord();
            var meaningOfDifferentWord = new Meaning
            {
                MeaningId = "MEANING-123",
                WordId = "DIFFERENT-WORD-ID"
            };

            _mockWordRepo.Setup(x => x.GetByIdAsync(command.WordId))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetByIdAsync(command.Meanings![0].MeaningId!))
                .ReturnsAsync(meaningOfDifferentWord);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.UpdateAsync(It.IsAny<Meaning>()), Times.Never);
        }
    }
    #endregion
  
}
