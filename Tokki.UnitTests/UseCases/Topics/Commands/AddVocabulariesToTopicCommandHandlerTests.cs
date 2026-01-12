using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class AddVocabulariesToTopicCommandHandlerTests : TopicTestBase
    {
        private readonly Mock<IValidator<AddVocabulariesToTopicCommand>> _mockValidator;
        private readonly AddVocabulariesToTopicCommandHandler _handler;

        public AddVocabulariesToTopicCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<AddVocabulariesToTopicCommand>>();

            _handler = new AddVocabulariesToTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockVocabRepo.Object,
                _mockVocabTopicRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return400_When_ValidationFails()
        {
            var cmd = TopicTestData.BuildAddVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
                {
                    new ValidationFailure("TopicId", "Required"){ ErrorCode="VALIDATION" }
                }));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            _mockTopicRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockVocabTopicRepo.Verify(x => x.AddOrReactivateVocabulariesToTopicAsync(
                It.IsAny<string>(), It.IsAny<List<Vocabulary>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildAddVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync((Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_NoValidVocabsFound()
        {
            var cmd = TopicTestData.BuildAddVocabsCommand(vocabIds: new List<string> { "x", "y" });

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(topicId: cmd.TopicId));

            _mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vocabulary>());

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_Return200_WithMessage_When_Success()
        {
            var cmd = TopicTestData.BuildAddVocabsCommand(
                vocabIds: new List<string> { "v1", "v2", "not-exist" });

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(topicId: cmd.TopicId, topicName: "T1"));

            var v1 = TopicTestData.BuildVocabulary("v1");
            var v2 = TopicTestData.BuildVocabulary("v2");

            _mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vocabulary> { v1, v2 });

            _mockVocabTopicRepo.Setup(x => x.AddOrReactivateVocabulariesToTopicAsync(
                    cmd.TopicId,
                    It.IsAny<List<Vocabulary>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((addedOrReactivated: 2, skippedAlreadyActive: 0, failedItems: new List<string>()));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);
            result.Message.Should().Contain("Kết quả thêm từ vựng vào chủ đề 'T1':");
            result.Message.Should().Contain("Không tồn tại trong hệ thống: 1");

            _mockVocabTopicRepo.Verify(x => x.AddOrReactivateVocabulariesToTopicAsync(
                cmd.TopicId, It.IsAny<List<Vocabulary>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
