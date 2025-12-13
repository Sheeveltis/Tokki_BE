
using Moq;
using Tokki.Application.UseCases.Words.Commands.BulkCreateWords;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using FluentAssertions;

namespace Tokki.UnitTests.UseCases.Word.Commands
{
    #region BulkCreateWords Tests
    public class BulkCreateWordsCommandHandlerTests : WordTestBase
    {
        private readonly BulkCreateWordsCommandHandler _handler;

        public BulkCreateWordsCommandHandlerTests()
        {
            _handler = new BulkCreateWordsCommandHandler(
                _mockWordRepo.Object,
                _mockMeaningRepo.Object,
                _mockTopicRepo.Object,
                _mockMeaningTopicRepo.Object,
                _mockIdGen.Object,
                _mockTtsService.Object,
                _mockCloudinaryService.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNotFound()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync((Tokki.Domain.Entities.Topic)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == "TOPIC_NOT_FOUND");
            _mockWordRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_CreateNewWords_When_WordsDoNotExist()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            var topic = WordTestData.GetFakeTopic();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetByTextAsync(It.IsAny<string>()))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);
            _mockIdGen.Setup(x => x.Generate(15))
                .Returns(() => Guid.NewGuid().ToString("N")[..15]);
            _mockTtsService.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockCloudinaryService.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.com/audio.mp3");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data.FailedCount.Should().Be(0);
            result.Data.TotalWords.Should().Be(2);

            _mockWordRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Exactly(2));
            _mockMeaningRepo.Verify(x => x.AddAsync(It.IsAny<Meaning>()), Times.Exactly(2));
            _mockMeaningTopicRepo.Verify(x => x.AddAsync(It.IsAny<MeaningTopic>()), Times.Exactly(2));
            _mockWordRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_AddMeaningsToExistingWord_When_WordExists()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            var topic = WordTestData.GetFakeTopic();
            var existingWord = WordTestData.GetFakeWord();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetByTextAsync(It.IsAny<string>()))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetMeaningByDefinitionAndTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Meaning?)null);
            _mockIdGen.Setup(x => x.Generate(15))
                .Returns(() => Guid.NewGuid().ToString("N")[..15]);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessCount.Should().Be(2);
            _mockWordRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Word>()), Times.Never);
            _mockMeaningRepo.Verify(x => x.AddAsync(It.IsAny<Meaning>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_Should_SkipDuplicateMeanings_When_MeaningAlreadyExistsInTopic()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            var topic = WordTestData.GetFakeTopic();
            var existingWord = WordTestData.GetFakeWord();
            var existingMeaning = WordTestData.GetFakeMeanings().First();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetByTextAsync(It.IsAny<string>()))
                .ReturnsAsync(existingWord);
            _mockMeaningRepo.Setup(x => x.GetMeaningByDefinitionAndTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingMeaning);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.AddAsync(It.IsAny<Meaning>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_HandlePartialSuccess_When_SomeWordsFailToCreate()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            var topic = WordTestData.GetFakeTopic();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync(topic);

            // First word succeeds
            _mockWordRepo.SetupSequence(x => x.GetByTextAsync(It.IsAny<string>()))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null)
                .ThrowsAsync(new Exception("Database error"));

            _mockIdGen.Setup(x => x.Generate(15))
                .Returns(() => Guid.NewGuid().ToString("N")[..15]);
            _mockTtsService.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockCloudinaryService.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.com/audio.mp3");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data.FailedCount.Should().Be(1);
            result.Data.Results.Should().HaveCount(2);
            result.Data.Results.Should().Contain(r => r.IsSuccess && r.Text == "안녕하세요");
            result.Data.Results.Should().Contain(r => !r.IsSuccess && r.Text == "감사합니다");
        }

        [Fact]
        public async Task Handle_Should_ContinueWithoutAudio_When_AudioGenerationFails()
        {
            // Arrange
            var command = WordTestData.GetValidBulkCreateWordsCommand();
            var topic = WordTestData.GetFakeTopic();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetByTextAsync(It.IsAny<string>()))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);
            _mockIdGen.Setup(x => x.Generate(15))
                .Returns(() => Guid.NewGuid().ToString("N")[..15]);
            _mockTtsService.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("TTS Service unavailable"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessCount.Should().Be(2);
            _mockWordRepo.Verify(x => x.AddAsync(It.Is<Tokki.Domain.Entities.Word>(w => w.AudioURL == null)), Times.Exactly(2));
        }
    }
    #endregion
}
