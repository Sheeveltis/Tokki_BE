using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Passages.Commands.CreatePassage;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Passages
{
    public class CreatePassageCommandHandlerTests
    {
        private static CreatePassageCommandHandler CreateHandler(
            Mock<IPassageRepository>? passageRepo = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            var mockId = idGen ?? new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.GenerateCustom(10)).Returns("PASS-001");
            return new CreatePassageCommandHandler(
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                mockId.Object);
        }

        private static CreatePassageCommand ValidCommand => new()
        {
            Title     = "Korean Listening Passage",
            Content   = "Some long text content here...",
            MediaType = Tokki.Domain.Enums.PassageMediaType.Text
        };

        // TC-01: Duplicate title ? 409
        [Fact]
        public async Task Handle_DuplicateTitle_ShouldReturn409()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(true);

            var result = await CreateHandler(repo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_01",
                Description = "Duplicate title ? 409 Conflict",
                ExpectedResult = "Return 409 PassageTitleDuplicated", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsTitleExistsAsync == true => 409" }
            });
        }

        // TC-02: Happy path Text type ? 201
        [Fact]
        public async Task Handle_ValidTextPassage_ShouldReturn201WithId()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var idGen = new Mock<IIdGeneratorService>();
            idGen.Setup(x => x.GenerateCustom(10)).Returns("PASS-NEW");

            var result = await CreateHandler(repo, idGen).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("PASS-NEW");

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_02",
                Description = "Valid Text passage ? AddAsync, SaveChanges, Return 201 with generated ID",
                ExpectedResult = "Return 201, Data='PASS-NEW'", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "title unique, type=Text => 201" }
            });
        }

        // TC-03: Image passage type ? 201
        [Fact]
        public async Task Handle_ValidImagePassage_ShouldReturn201()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new CreatePassageCommand
            {
                Title     = "Image Passage",
                ImageUrl  = "https://cdn.example.com/image.jpg",
                MediaType = Tokki.Domain.Enums.PassageMediaType.Image
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_03",
                Description = "Valid Image passage with ImageUrl ? 201",
                ExpectedResult = "Return 201", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "type=Image, ImageUrl set => 201" }
            });
        }

        // TC-04: Audio passage type ? 201
        [Fact]
        public async Task Handle_ValidAudioPassage_ShouldReturn201()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new CreatePassageCommand
            {
                Title     = "Audio Passage",
                AudioUrl  = "https://cdn.example.com/audio.mp3",
                MediaType = Tokki.Domain.Enums.PassageMediaType.Audio
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_04",
                Description = "Valid Audio passage with AudioUrl ? 201",
                ExpectedResult = "Return 201", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "type=Audio, AudioUrl set => 201" }
            });
        }

        // TC-05: AddAsync throws ? 500
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Passage>())).ThrowsAsync(new Exception("DB error"));

            var result = await CreateHandler(repo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_05",
                Description = "AddAsync throws ? catch ? 500 ServerError",
                ExpectedResult = "Return 500 Failure", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws => 500" }
            });
        }

        // TC-06: Title is trimmed before check
        [Fact]
        public async Task Handle_TitleWithWhitespace_ShouldBeTrimmed()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.IsTitleExistsAsync("Trimmed Title", It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Passage>())).Returns(Task.CompletedTask);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var cmd = new CreatePassageCommand
            {
                Title     = "  Trimmed Title",
                Content   = "content",
                MediaType = Tokki.Domain.Enums.PassageMediaType.Text
            };

            var result = await CreateHandler(repo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.IsTitleExistsAsync("Trimmed Title", It.IsAny<string?>()), Times.Once);

            QACollector.LogTestCase("Passage - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePassage", TestCaseID = "CreatePassage_06",
                Description = "Title with leading/trailing whitespace ? trimmed before check",
                ExpectedResult = "IsTitleExistsAsync called with 'Trimmed Title'", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Title.Trim() used" }
            });
        }
    }
}
