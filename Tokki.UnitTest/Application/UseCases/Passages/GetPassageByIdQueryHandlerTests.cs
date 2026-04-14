using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Passages.DTOs;
using Tokki.Application.UseCases.Passages.Queries.GetPassageById;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Passages
{
    public class GetPassageByIdQueryHandlerTests
    {
        private static GetPassageByIdQueryHandler CreateHandler(
            Mock<IPassageRepository>? passageRepo = null)
        {
            return new GetPassageByIdQueryHandler(
                (passageRepo ?? new Mock<IPassageRepository>()).Object);
        }

        private static Passage SamplePassage(string id) => new()
        {
            PassageId = id,
            Title     = "Korean Grammar",
            Content   = "이것은 내용입니다.",
            Status    = PassageStatus.Active,
            MediaType = PassageMediaType.Text,
            CreatedAt = new DateTime(2025, 1, 15)
        };

        // TC-01: Not found → 404
        [Fact]
        public async Task Handle_NotFound_ShouldReturn404()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-999", It.IsAny<CancellationToken>())).ReturnsAsync((Passage?)null);

            var result = await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "P-999" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-01",
                Description = "Passage not found → 404",
                ExpectedResult = "Return 404 PassageNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passage == null => 404" }
            });
        }

        // TC-02: Happy path → 200 DTO mapped
        [Fact]
        public async Task Handle_Found_ShouldReturn200WithDto()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(SamplePassage("P-001"));

            var result = await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.PassageId.Should().Be("P-001");
            result.Data.Title.Should().Be("Korean Grammar");
            result.Data.Status.Should().Be(PassageStatus.Active);
            result.Data.MediaType.Should().Be(PassageMediaType.Text);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-02",
                Description = "Passage found → 200, DTO fields mapped correctly",
                ExpectedResult = "Return 200, DTO mapped", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passage found => dto mapped => 200" }
            });
        }

        // TC-03: PassageId is trimmed before query
        [Fact]
        public async Task Handle_PassageIdWithSpaces_ShouldBeTrimmed()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Passage?)null);

            await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "  P-001  " }, CancellationToken.None);

            repo.Verify(x => x.GetByIdAsync("P-001", It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-03",
                Description = "PassageId with whitespace → trimmed before repo call",
                ExpectedResult = "GetByIdAsync('P-001') called", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.PassageId.Trim() used" }
            });
        }

        // TC-04: Audio passage type mapped in DTO
        [Fact]
        public async Task Handle_AudioPassage_ShouldMapAudioUrl()
        {
            var audio = new Passage
            {
                PassageId = "P-002",
                Title     = "Audio Test",
                AudioUrl  = "https://cdn.example.com/audio.mp3",
                MediaType = PassageMediaType.Audio,
                Status    = PassageStatus.Active
            };

            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-002", It.IsAny<CancellationToken>())).ReturnsAsync(audio);

            var result = await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "P-002" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.AudioUrl.Should().Be("https://cdn.example.com/audio.mp3");
            result.Data.MediaType.Should().Be(PassageMediaType.Audio);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-04",
                Description = "Audio passage → AudioUrl and MediaType=Audio in DTO",
                ExpectedResult = "Return 200, AudioUrl and MediaType correct", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "type=Audio => AudioUrl mapped" }
            });
        }

        // TC-05: Image passage type mapped in DTO
        [Fact]
        public async Task Handle_ImagePassage_ShouldMapImageUrl()
        {
            var img = new Passage
            {
                PassageId = "P-003",
                Title     = "Image Passage",
                ImageUrl  = "https://cdn.example.com/img.jpg",
                MediaType = PassageMediaType.Image,
                Status    = PassageStatus.Active
            };

            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync("P-003", It.IsAny<CancellationToken>())).ReturnsAsync(img);

            var result = await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "P-003" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.ImageUrl.Should().Be("https://cdn.example.com/img.jpg");
            result.Data.MediaType.Should().Be(PassageMediaType.Image);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-05",
                Description = "Image passage → ImageUrl and MediaType=Image in DTO",
                ExpectedResult = "Return 200, ImageUrl set", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "type=Image => ImageUrl mapped" }
            });
        }

        // TC-06: Repository throws → 500
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var repo = new Mock<IPassageRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

            var result = await CreateHandler(repo)
                .Handle(new GetPassageByIdQuery { PassageId = "P-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Passage - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetPassageById", TestCaseID = "TC-PAS-GID-06",
                Description = "Repository throws → catch → 500",
                ExpectedResult = "Return 500 ServerError", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws => 500" }
            });
        }
    }
}
