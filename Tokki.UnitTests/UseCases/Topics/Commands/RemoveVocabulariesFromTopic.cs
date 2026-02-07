using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class RemoveVocabulariesFromTopicCommandHandlerTests : TopicTestBase
    {
        private readonly Mock<IValidator<RemoveVocabulariesFromTopicCommand>> _mockValidator;
        private readonly RemoveVocabulariesFromTopicCommandHandler _handler;

        public RemoveVocabulariesFromTopicCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<RemoveVocabulariesFromTopicCommand>>();

            _handler = new RemoveVocabulariesFromTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockVocabTopicRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return400_When_ValidationFails()
        {
            var cmd = TopicTestData.BuildRemoveVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
                {
                    new ValidationFailure("TopicId","Required"){ ErrorCode="VALIDATION" }
                }));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildRemoveVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync((Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_RepoTransactionFailed()
        {
            var cmd = TopicTestData.BuildRemoveVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(topicName: "T1"));

            _mockVocabTopicRepo.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                    cmd.TopicId,
                    cmd.VocabularyIds,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((success: false, removedCount: 0, failedItems: new List<string> { "v1" }));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Giao dịch đã bị hủy");
        }

        [Fact]
        public async Task Handle_Should_Return200_When_NothingRemoved()
        {
            var cmd = TopicTestData.BuildRemoveVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(topicName: "T1"));

            _mockVocabTopicRepo.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                    cmd.TopicId,
                    cmd.VocabularyIds,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((success: true, removedCount: 0, failedItems: new List<string>()));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Không có thay đổi");
        }

        [Fact]
        public async Task Handle_Should_Return200_When_Removed()
        {
            var cmd = TopicTestData.BuildRemoveVocabsCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(topicName: "T1"));

            _mockVocabTopicRepo.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                    cmd.TopicId,
                    cmd.VocabularyIds,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((success: true, removedCount: 2, failedItems: new List<string>()));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);
            result.Message.Should().Contain("Đã gỡ thành công 2");
        }
    }
}
