using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class PublishTopicCommandHandlerTests : TopicTestBase
    {
        private readonly Mock<IValidator<PublishTopicCommand>> _mockValidator;
        private readonly PublishTopicCommandHandler _handler;

        public PublishTopicCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<PublishTopicCommand>>();

            _handler = new PublishTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return400_When_ValidationFails()
        {
            var cmd = TopicTestData.BuildPublishCommand();

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
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();
            var cmd = TopicTestData.BuildPublishCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildPublishCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync((Tokki.Domain.Entities.Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return200_When_AlreadyActive()
        {
            var cmd = TopicTestData.BuildPublishCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Active));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Handle_Should_PublishDraftToActive()
        {
            var cmd = TopicTestData.BuildPublishCommand();

            _mockValidator.Setup(v => v.ValidateAsync(cmd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var topic = TopicTestData.BuildTopic(status: TopicStatus.Draft);
            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);

            _mockTopicRepo.Setup(x => x.UpdateAsync(topic)).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Active);

            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
