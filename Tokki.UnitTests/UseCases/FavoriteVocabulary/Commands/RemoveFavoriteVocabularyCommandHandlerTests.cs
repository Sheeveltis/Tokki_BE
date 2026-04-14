using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary;
using Tokki.Application.UseCases.UserFavoriteVocabularies.Commands.RemoveFavoriteVocabulary;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.FavoriteVocabulary.Commands
{
    public class RemoveFavoriteVocabularyCommandHandlerTests : FavoriteVocabularyTestBase
    {
        private readonly Mock<IValidator<RemoveFavoriteVocabularyCommand>> _mockValidator;
        private readonly RemoveFavoriteVocabularyCommandHandler _handler;

        public RemoveFavoriteVocabularyCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<RemoveFavoriteVocabularyCommand>>();

            _handler = new RemoveFavoriteVocabularyCommandHandler(
                _mockFavoriteRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ValidationFails()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetRemoveCommand();

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
                          {
                              new ValidationFailure("VocabularyId", "Required")
                          }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            _mockFavoriteRepo.Verify(x => x.HardDeleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_When_UserNotLoggedIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            var command = FavoriteVocabularyTestData.GetRemoveCommand();

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult()); // valid

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            _mockFavoriteRepo.Verify(x => x.HardDeleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_FavoriteNotExists()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetRemoveCommand();

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult()); // valid

            _mockFavoriteRepo.Setup(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Vocabulary does not exist in the favorites list.");

            _mockFavoriteRepo.Verify(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_FavoriteDeleted()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetRemoveCommand();

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult()); // valid

            _mockFavoriteRepo.Setup(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Successfully removed from favorites list.");

            _mockFavoriteRepo.Verify(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RepositoryThrows()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetRemoveCommand();

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult()); // valid

            _mockFavoriteRepo.Setup(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                             .ThrowsAsync(new System.Exception("db error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.FavoriteVocabularyRemoveFailed.Code);

            _mockFavoriteRepo.Verify(x => x.HardDeleteAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
